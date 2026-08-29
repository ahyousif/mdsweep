using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Mdsweep.Infrastructure.DriverWork;

public static class DriverWorkHandler
{
    private static readonly string[] OutcomeReasons =
    [
        "PassengerNoShow",
        "PassengerCancelled",
        "UnableToLocatePassenger",
        "VehicleIssue",
        "Other",
    ];

    public static async Task<DriverWorkResult<IReadOnlyList<DriverTripResponse>>> Handle(
        ListDriverTrips query,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(query.ProviderId, query.AppUserId, db, cancellationToken);
        if (driver is null)
        {
            return new DriverWorkResult<IReadOnlyList<DriverTripResponse>>(
                DriverWorkOutcome.Forbid
            );
        }

        var events = db.DriverTripEvents;
        var trips = await (
            from assignment in db.TripAssignments
            join trip in db.Trips on assignment.TripId equals trip.Id
            where
                assignment.DriverId == driver.Id
                && assignment.SupersededAt == null
                && trip.IsActive
                && trip.AppointmentDate == DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)
            orderby trip.AppointmentDate, trip.AppointmentTime, trip.TripNumber
            select new DriverTripResponse(
                trip.TripNumber,
                trip.JourneyKey,
                trip.AppointmentDate,
                trip.AppointmentTime,
                trip.MemberFirstName + " " + trip.MemberLastName,
                trip.PassengerType,
                trip.VehicleType,
                trip.PickupAddress,
                trip.PickupCity,
                trip.DeliveryAddress,
                trip.DeliveryCity,
                trip.PassengerPhone,
                events
                    .Where(x => x.TripId == trip.Id)
                    .OrderByDescending(x => x.ReceivedAt)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (DriverTripEventType?)x.Type)
                    .FirstOrDefault()
            )
        ).ToListAsync(cancellationToken);

        return new DriverWorkResult<IReadOnlyList<DriverTripResponse>>(
            DriverWorkOutcome.Success,
            trips.Select(x => x with { NextAction = NextAction(x.LastEventType) }).ToArray()
        );
    }

    public static async Task<DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>> Handle(
        GetDriverTripHistory query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(query.ProviderId, query.AppUserId, db, cancellationToken);
        if (driver is null)
        {
            return new DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>(
                DriverWorkOutcome.Forbid
            );
        }

        var trip = await AssignedTrip(driver.Id, query.TripNumber, db, cancellationToken);
        if (trip is null)
        {
            return new DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>(
                DriverWorkOutcome.NotFound
            );
        }

        var events = await db
            .DriverTripEvents.Where(x => x.TripId == trip.Id)
            .OrderBy(x => x.ReceivedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var ids = events.Select(x => x.Id).ToArray();
        var corrections = await db
            .DriverTripEventCorrections.Where(x => ids.Contains(x.DriverTripEventId))
            .OrderBy(x => x.ReceivedAt)
            .ToListAsync(cancellationToken);
        var response = events
            .Select(x => new DriverTripEventResponse(
                x.Id,
                x.Type,
                x.DeviceCapturedAt,
                x.ReceivedAt,
                x.OutcomeReason,
                x.Note,
                x.TripLogSigned,
                corrections
                    .Where(c => c.DriverTripEventId == x.Id)
                    .Select(c => new DriverTripEventCorrectionResponse(
                        c.Id,
                        c.DriverTripEventId,
                        c.CorrectedDeviceCapturedAt,
                        c.ReceivedAt,
                        c.Reason
                    ))
                    .ToList()
            ))
            .ToArray();

        return new DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>(
            DriverWorkOutcome.Success,
            response
        );
    }

    [Transactional]
    public static async Task<DriverWorkResult<DriverTripEventResponse>> Handle(
        RecordDriverTripEvent command,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(
            command.ProviderId,
            command.AppUserId,
            db,
            cancellationToken
        );
        if (driver is null)
        {
            return new DriverWorkResult<DriverTripEventResponse>(DriverWorkOutcome.Forbid);
        }

        var trip = await AssignedTrip(driver.Id, command.TripNumber, db, cancellationToken);
        if (trip is null)
        {
            return new DriverWorkResult<DriverTripEventResponse>(DriverWorkOutcome.NotFound);
        }

        return await Record(
            driver,
            trip,
            command.TripNumber,
            command.Event,
            db,
            clock,
            cancellationToken
        );
    }

    [Transactional]
    public static async Task<DriverWorkResult<DriverTripEventResponse>> Handle(
        SynchronizeDriverTripEvent command,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(
            command.ProviderId,
            command.AppUserId,
            db,
            cancellationToken
        );
        if (driver is null)
        {
            return new DriverWorkResult<DriverTripEventResponse>(DriverWorkOutcome.Forbid);
        }

        var request = command.Request;
        var existingConflict = await db.DriverTripSyncConflicts.SingleOrDefaultAsync(
            x => x.DriverId == driver.Id && x.ActionId == request.ActionId,
            cancellationToken
        );
        if (existingConflict is not null)
        {
            return new DriverWorkResult<DriverTripEventResponse>(
                DriverWorkOutcome.Conflict,
                Message: existingConflict.Reason
            );
        }

        var trip = await AssignedTrip(driver.Id, request.TripNumber, db, cancellationToken);
        if (trip is not null)
        {
            return await Record(
                driver,
                trip,
                request.TripNumber,
                request.Event,
                db,
                clock,
                cancellationToken
            );
        }

        db.DriverTripSyncConflicts.Add(
            new DriverTripSyncConflict
            {
                ProviderId = command.ProviderId,
                DriverId = driver.Id,
                ActionId = request.ActionId,
                TripNumber = request.TripNumber,
                Type = request.Event.Type,
                DeviceCapturedAt = request.Event.DeviceCapturedAt,
                ReceivedAt = clock.UtcNow,
                Reason = "Trip is no longer assigned to this Driver.",
                TripLogSigned = request.Event.TripLogSigned,
                OutcomeReason = request.Event.OutcomeReason?.Trim(),
                Note = request.Event.Note?.Trim(),
            }
        );

        return new DriverWorkResult<DriverTripEventResponse>(
            DriverWorkOutcome.Conflict,
            Message: "This queued action needs Dispatcher attention because the Trip is no longer assigned to you."
        );
    }

    public static Task<List<DriverTripSyncConflictResponse>> Handle(
        ListDriverSyncConflicts query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .DriverTripSyncConflicts.Where(x => x.ProviderId == query.ProviderId)
            .OrderByDescending(x => x.ReceivedAt)
            .Select(x => new DriverTripSyncConflictResponse(
                x.Id,
                x.TripNumber,
                x.Type,
                x.DeviceCapturedAt,
                x.ReceivedAt,
                x.Reason,
                x.TripLogSigned,
                x.OutcomeReason,
                x.Note
            ))
            .ToListAsync(cancellationToken);

    [Transactional]
    public static async Task<DriverWorkResult<DriverTripEventCorrectionResponse>> Handle(
        CorrectDriverTripEvent command,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(
            command.ProviderId,
            command.AppUserId,
            db,
            cancellationToken
        );
        if (driver is null)
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.Forbid
            );
        }

        var request = command.Correction;
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.BadRequest,
                Message: "A correction reason is required."
            );
        }

        if (request.DeviceCapturedAt == default)
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.BadRequest,
                Message: "Corrected device capture time is required."
            );
        }

        var trip = await AssignedTrip(driver.Id, command.TripNumber, db, cancellationToken);
        if (trip is null)
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.NotFound
            );
        }

        var original = await db.DriverTripEvents.SingleOrDefaultAsync(
            x => x.Id == command.EventId && x.TripId == trip.Id && x.DriverId == driver.Id,
            cancellationToken
        );
        if (original is null)
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.NotFound
            );
        }

        if (
            original.ReceivedAt > clock.UtcNow
            || clock.UtcNow - original.ReceivedAt > TimeSpan.FromMinutes(15)
        )
        {
            return new DriverWorkResult<DriverTripEventCorrectionResponse>(
                DriverWorkOutcome.BadRequest,
                Message: "This event is no longer eligible for a Driver correction. Ask a Dispatcher for help."
            );
        }

        var correction = new DriverTripEventCorrection
        {
            DriverTripEventId = original.Id,
            CorrectedByDriverId = driver.Id,
            CorrectedDeviceCapturedAt = request.DeviceCapturedAt,
            ReceivedAt = clock.UtcNow,
            Reason = request.Reason.Trim(),
        };
        db.DriverTripEventCorrections.Add(correction);

        return new DriverWorkResult<DriverTripEventCorrectionResponse>(
            DriverWorkOutcome.Success,
            new DriverTripEventCorrectionResponse(
                correction.Id,
                correction.DriverTripEventId,
                correction.CorrectedDeviceCapturedAt,
                correction.ReceivedAt,
                correction.Reason
            ),
            Location: $"/api/driver-work/trips/{command.TripNumber}/history"
        );
    }

    private static async Task<DriverWorkResult<DriverTripEventResponse>> Record(
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

    private static DriverTripEventResponse ToResponse(DriverTripEvent value) =>
        new(
            value.Id,
            value.Type,
            value.DeviceCapturedAt,
            value.ReceivedAt,
            value.OutcomeReason,
            value.Note,
            value.TripLogSigned
        );

    private static Task<Driver?> ResolveDriver(
        Guid providerId,
        Guid appUserId,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db.Drivers.SingleOrDefaultAsync(
            x => x.ProviderId == providerId && x.AppUserId == appUserId && x.IsActive,
            cancellationToken
        );

    private static Task<Trip?> AssignedTrip(
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

    private static DriverTripEventType? NextAction(DriverTripEventType? lastEvent) =>
        lastEvent switch
        {
            null => DriverTripEventType.ArrivedAtPickup,
            DriverTripEventType.ArrivedAtPickup => DriverTripEventType.PickedUp,
            DriverTripEventType.PickedUp => DriverTripEventType.ArrivedAtDropOff,
            DriverTripEventType.ArrivedAtDropOff => DriverTripEventType.DroppedOff,
            _ => null,
        };

    private static string Display(DriverTripEventType type) =>
        type switch
        {
            DriverTripEventType.ArrivedAtPickup => "Arrived at Pickup",
            DriverTripEventType.PickedUp => "Picked Up",
            DriverTripEventType.ArrivedAtDropOff => "Arrived at Drop-Off",
            DriverTripEventType.DroppedOff => "Dropped Off",
            _ => "Could Not Complete",
        };

    private static bool SameEvent(
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
