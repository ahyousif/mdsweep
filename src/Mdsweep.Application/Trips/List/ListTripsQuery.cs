using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsQuery(
    DateOnly? ServiceDate = null,
    string? BrokerStatus = null,
    bool? IsWillCall = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IQuery<PagedResult<TripModel>>;
