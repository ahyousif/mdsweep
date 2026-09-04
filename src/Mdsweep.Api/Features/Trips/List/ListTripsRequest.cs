using Mdsweep.Application.Common.Models;
using Mdsweep.Application.Trips;
using Mdsweep.Application.Trips.List;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequest
{
    // Backwards-compatible single-day query parameter for existing callers.
    public DateOnly? ServiceDate { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Search { get; set; }

    public bool? NeedsAttention { get; set; }

    public string? BrokerStatus { get; set; }

    public bool? IsWillCall { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public TripSortBy SortBy { get; set; } = TripSortBy.ScheduledPickupTime;

    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    public ListTripsQuery ToQuery()
    {
        var startDate = StartDate.HasValue ? LocalDate.FromDateOnly(StartDate.Value) : ServiceDate.HasValue ? LocalDate.FromDateOnly(ServiceDate.Value) : (LocalDate?)null;
        var endDate = EndDate.HasValue ? LocalDate.FromDateOnly(EndDate.Value) : ServiceDate.HasValue ? LocalDate.FromDateOnly(ServiceDate.Value) : (LocalDate?)null;

        return new ListTripsQuery(startDate, endDate, Search, NeedsAttention, BrokerStatus, IsWillCall, Page, PageSize, SortBy, SortDirection);
    }
}
