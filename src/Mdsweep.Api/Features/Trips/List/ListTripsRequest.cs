using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Trips;
using Mdsweep.Application.Trips.List;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequest
{
    public LocalDate? ServiceDate { get; init; }

    public string? BrokerStatus { get; init; }

    public bool? IsWillCall { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    public TripSortBy SortBy { get; init; } = TripSortBy.AppointmentTime;

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    public ListTripsQuery ToQuery()
    {
        return new ListTripsQuery(ServiceDate, BrokerStatus, IsWillCall, Page, PageSize, SortBy, SortDirection);
    }
}
