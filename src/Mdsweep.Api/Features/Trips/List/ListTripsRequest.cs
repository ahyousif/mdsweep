using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Trips;
using Mdsweep.Application.Trips.List;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequest
{
    public DateOnly? ServiceDate { get; set; }

    public string? BrokerStatus { get; set; }

    public bool? IsWillCall { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public TripSortBy SortBy { get; set; } = TripSortBy.AppointmentTime;

    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    public ListTripsQuery ToQuery()
    {
        var serviceDate = ServiceDate.HasValue ? LocalDate.FromDateOnly(ServiceDate.Value) : (LocalDate?)null;

        return new ListTripsQuery(serviceDate, BrokerStatus, IsWillCall, Page, PageSize, SortBy, SortDirection);
    }
}
