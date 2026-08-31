using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.DriverWork;

internal static class DriverWorkHandlerSupport
{
    internal static readonly string[] OutcomeReasons =
    [
        "PassengerNoShow",
        "PassengerCancelled",
        "UnableToLocatePassenger",
        "VehicleIssue",
        "Other",
    ];

    internal static async Task<DriverWorkResult<DriverTripEventResponse>> Record(
        Driver driver,
        Trip trip,
        string tripNumber,
        RecordDriverTripEventRequest request,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        if (request.DeviceCapturedAt == default)
        {
            return Invalid("Device capture time is required.");
        }

        if (request.Type == DriverTripEventType.DroppedOff && request.TripLogSigned is null)
        {
            return Invalid("Record whether the Trip Log was signed before completing the Trip.");
        }

        if (
            request.Type == DriverTripEventType.CouldNotComplete
            && !OutcomeReasons.Contains(request.OutcomeReason, StringComparer.Ordinal)
        )
        {
            return Invalid("Choose a standardized Could Not Complete reason.");
        }

        if (
            request.Type != DriverTripEventType.CouldNotComplete
            && (
                !string.IsNullOrWhiteSpace(request.OutcomeReason)
                || !string.IsNullOrWhiteSpace(request.Note)
            )
        )
        {
            return Invalid(
                "An outcome reason and note may only be recorded for Could Not Complete."
            );
        }

        var existing = await db.DriverTripEvents.SingleOrDefaultAsync(
            x => x.TripId == trip.Id && x.DeviceCapturedAt == request.DeviceCapturedAt,
            cancellationToken
        );
        if (existing is not null)
        {
            if (SameEvent(existing, driver.Id, request))
            {
                return new DriverWorkResult<DriverTripEventResponse>(
                    DriverWorkOutcome.Success,
                    ToResponse(existing)
                );
            }

            return new DriverWorkResult<DriverTripEventResponse>(
                DriverWorkOutcome.Conflict,
                Message: "An event with this device capture time was already recorded with different details."
            );
        }

        var lastEvent = await db
            .DriverTripEvents.Where(x => x.TripId == trip.Id)
            .OrderByDescending(x => x.ReceivedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var expected = NextAction(lastEvent?.Type);
        if (
            expected is null
            || (request.Type != expected && request.Type != DriverTripEventType.CouldNotComplete)
        )
        {
            return Invalid(
                expected is null
                    ? "This Trip already has a physical outcome and cannot receive another event."
                    : $"Record {Display(expected.Value)} before {Display(request.Type)}."
            );
        }

        if (lastEvent is not null && request.DeviceCapturedAt < lastEvent.DeviceCapturedAt)
        {
            return Invalid(
                "Device capture time must not be earlier than the preceding Trip event."
            );
        }

        var recorded = new DriverTripEvent
        {
            TripId = trip.Id,
            DriverId = driver.Id,
            Type = request.Type,
            DeviceCapturedAt = request.DeviceCapturedAt,
            ReceivedAt = clock.UtcNow,
            OutcomeReason = request.OutcomeReason?.Trim(),
            Note = request.Note?.Trim(),
            TripLogSigned = request.TripLogSigned,
        };
        db.DriverTripEvents.Add(recorded);

        return new DriverWorkResult<DriverTripEventResponse>(
            DriverWorkOutcome.Success,
            ToResponse(recorded),
            Location: $"/api/driver-work/trips/{tripNumber}/history"
        );

        static DriverWorkResult<DriverTripEventResponse> Invalid(string message) =>
            new(DriverWorkOutcome.BadRequest, Message: message);
    }

    internal static DriverTripEventResponse ToResponse(DriverTripEvent value) =>
        new(
            value.Id,
            value.Type,
            value.DeviceCapturedAt,
            value.ReceivedAt,
            value.OutcomeReason,
            value.Note,
            value.TripLogSigned
        );

    internal static Task<Driver?> ResolveDriver(
        Guid providerId,
        Guid appUserId,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db.Drivers.SingleOrDefaultAsync(
            x => x.ProviderId == providerId && x.AppUserId == appUserId && x.IsActive,
            cancellationToken
        );

    internal static Task<Trip?> AssignedTrip(
        Guid driverId,
        string tripNumber,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        (
            from assignment in db.TripAssignments
            join trip in db.Trips on assignment.TripId equals trip.Id
            where
                assignment.DriverId == driverId
                && assignment.SupersededAt == null
                && trip.TripNumber == tripNumber
                && trip.IsActive
            select trip
        ).SingleOrDefaultAsync(cancellationToken);

    internal static DriverTripEventType? NextAction(DriverTripEventType? lastEvent) =>
        lastEvent switch
        {
            null => DriverTripEventType.ArrivedAtPickup,
            DriverTripEventType.ArrivedAtPickup => DriverTripEventType.PickedUp,
            DriverTripEventType.PickedUp => DriverTripEventType.ArrivedAtDropOff,
            DriverTripEventType.ArrivedAtDropOff => DriverTripEventType.DroppedOff,
            _ => null,
        };

    internal static string Display(DriverTripEventType type) =>
        type switch
        {
            DriverTripEventType.ArrivedAtPickup => "Arrived at Pickup",
            DriverTripEventType.PickedUp => "Picked Up",
            DriverTripEventType.ArrivedAtDropOff => "Arrived at Drop-Off",
            DriverTripEventType.DroppedOff => "Dropped Off",
            _ => "Could Not Complete",
        };

    internal static bool SameEvent(
        DriverTripEvent existing,
        Guid driverId,
        RecordDriverTripEventRequest request
    ) =>
        existing.DriverId == driverId
        && existing.Type == request.Type
        && existing.TripLogSigned == request.TripLogSigned
        && string.Equals(
            existing.OutcomeReason,
            request.OutcomeReason?.Trim(),
            StringComparison.Ordinal
        )
        && string.Equals(existing.Note, request.Note?.Trim(), StringComparison.Ordinal);
}
