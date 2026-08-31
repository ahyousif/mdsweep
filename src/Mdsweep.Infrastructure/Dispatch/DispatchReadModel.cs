using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class DispatchReadModel
{
    public static async Task<HashSet<Guid>> GetTripIdsWithOperationalOverrides(
        ApplicationDbContext db,
        IEnumerable<Guid> tripIds,
        CancellationToken cancellationToken
    )
    {
        var ids = tripIds.ToArray();
        return await db
            .TripSchedules.AsNoTracking()
            .Where(schedule => ids.Contains(schedule.TripId))
            .Select(schedule => schedule.TripId)
            .ToHashSetAsync(cancellationToken);
    }
}
