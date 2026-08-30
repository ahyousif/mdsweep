using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;
using static Mdsweep.Infrastructure.DriverWork.DriverWorkHandlerSupport;

namespace Mdsweep.Infrastructure.DriverWork;

public static class DriverTripEventCorrectionHandler
{
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
}
