using Mdsweep.Application.Dispatch;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class SetScheduledPickupTimeHandler
{
    [Transactional]
    public static async Task<SetScheduledPickupTimeResult> Handle(
        SetScheduledPickupTime command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var trip = await db.Trips.SingleOrDefaultAsync(
            x => x.ProviderId == command.ProviderId && x.TripNumber == command.TripNumber,
            cancellationToken
        );
        if (trip is null)
        {
            return new SetScheduledPickupTimeResult(
                SetScheduledPickupTimeOutcome.NotFound,
                command.ScheduledPickupTime
            );
        }

        if (!trip.IsActive)
        {
            return new SetScheduledPickupTimeResult(
                SetScheduledPickupTimeOutcome.Inactive,
                command.ScheduledPickupTime
            );
        }

        var schedule = await db.TripSchedules.FindAsync([trip.Id], cancellationToken);
        if (schedule?.ScheduledPickupTime == command.ScheduledPickupTime)
        {
            return new SetScheduledPickupTimeResult(
                SetScheduledPickupTimeOutcome.Updated,
                command.ScheduledPickupTime
            );
        }

        if (schedule is null)
        {
            schedule = new TripSchedule
            {
                TripId = trip.Id,
                ScheduledPickupTime = command.ScheduledPickupTime,
            };
            db.TripSchedules.Add(schedule);
        }
        else
        {
            schedule.ScheduledPickupTime = command.ScheduledPickupTime;
        }

        db.ScheduledPickupTimeChanges.Add(
            new ScheduledPickupTimeChange
            {
                TripId = trip.Id,
                ScheduledPickupTime = command.ScheduledPickupTime,
                ChangedBy = command.AppUserId.ToString(),
            }
        );

        return new SetScheduledPickupTimeResult(
            SetScheduledPickupTimeOutcome.Updated,
            command.ScheduledPickupTime
        );
    }
}
