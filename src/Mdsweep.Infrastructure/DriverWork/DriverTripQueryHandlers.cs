using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;
using static Mdsweep.Infrastructure.DriverWork.DriverWorkHandlerSupport;

namespace Mdsweep.Infrastructure.DriverWork;

public static class DriverTripQueryHandler
{
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
}
