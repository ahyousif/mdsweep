using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsQuery(
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    string? Search = null,
    string? BrokerStatus = null,
    bool? IsWillCall = null,
    int Page = 1,
    int PageSize = 50
) : IQuery<ListTripsResult>;
