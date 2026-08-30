using Mdsweep.Application.Dispatch;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mdsweep.Infrastructure.Dispatch;

public static class GetScheduledPickupTimeHistoryHandler
{
    public static async Task<GetScheduledPickupTimeHistoryResult> Handle(
        GetScheduledPickupTimeHistory query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var tripId = await db
            .Trips.Where(x => x.ProviderId == query.ProviderId && x.TripNumber == query.TripNumber)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!tripId.HasValue)
        {
            return new GetScheduledPickupTimeHistoryResult(false, []);
        }

        var changes = await db
            .ScheduledPickupTimeChanges.Where(x => x.TripId == tripId.Value)
            .OrderBy(x => x.Sequence)
            .Select(x => new ScheduledPickupTimeChangeResponse(
                x.Sequence,
                x.ScheduledPickupTime,
                x.ChangedAt,
                x.ChangedBy
            ))
            .ToListAsync(cancellationToken);

        return new GetScheduledPickupTimeHistoryResult(true, changes);
    }
}
