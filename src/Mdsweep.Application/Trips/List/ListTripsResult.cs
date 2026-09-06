namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsResult(
    IReadOnlyList<TripModel> Items,
    long TotalCount,
    int Page,
    int PageSize,
    long TotalPages,
    long ScopeCount,
    long AttentionCount
);
