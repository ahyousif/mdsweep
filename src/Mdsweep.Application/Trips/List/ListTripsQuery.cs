using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Pagination;
using ApplicationPagedResult = Mdsweep.Application.Common.Pagination.PagedResult<Mdsweep.Application.Trips.TripModel>;

namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsQuery(
    LocalDate? ServiceDate = null,
    string? BrokerStatus = null,
    bool? IsWillCall = null,
    int Page = 1,
    int PageSize = 50,
    TripSortBy SortBy = TripSortBy.AppointmentTime,
    SortDirection SortDirection = SortDirection.Ascending
) : IQuery<ApplicationPagedResult>;

public enum TripSortBy
{
    AppointmentTime,
    ServiceDate,
    BrokerTripNumber,
    ScheduledPickupTime,
}
