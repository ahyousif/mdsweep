using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;
using static Mdsweep.Infrastructure.DriverWork.DriverWorkHandlerSupport;

namespace Mdsweep.Infrastructure.DriverWork;

public static class DriverTripEventHandler
{
    [Transactional]
    public static async Task<DriverWorkResult<DriverTripEventResponse>> Handle(
        RecordDriverTripEvent command,
        ApplicationDbContext db,
        IDriverWorkClock clock,
        CancellationToken cancellationToken
    )
    {
        var driver = await ResolveDriver(
            command.TenantId,
            command.UserId,
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
            command.TenantId,
            command.UserId,
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
                TenantId = command.TenantId,
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
            .DriverTripSyncConflicts.Where(x => x.TenantId == query.TenantId)
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
}
