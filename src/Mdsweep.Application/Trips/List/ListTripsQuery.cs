using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Models;

namespace Mdsweep.Application.Trips.List;

public sealed record ListTripsQuery(
    LocalDate? ServiceDate = null,
    string? BrokerStatus = null,
    bool? IsWillCall = null,
    int Page = 1,
    int PageSize = 50,
    TripSortBy SortBy = TripSortBy.AppointmentTime,
    SortDirection SortDirection = SortDirection.Asc
) : IQuery<PagedResult<IReadOnlyList<TripModel>>>;
