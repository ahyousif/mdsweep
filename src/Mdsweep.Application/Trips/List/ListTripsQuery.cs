using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Models;

namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsQuery(
    LocalDate? StartDate = null,
    LocalDate? EndDate = null,
    string? Search = null,
    bool? NeedsAttention = null,
    string? BrokerStatus = null,
    bool? IsWillCall = null,
    int Page = 1,
    int PageSize = 50,
    TripSortBy SortBy = TripSortBy.ScheduledPickupTime,
    SortDirection SortDirection = SortDirection.Ascending
) : IQuery<ListTripsResult>;
