using Mdsweep.Application.Trips.List;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequest
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Search { get; set; }

    public string? BrokerStatus { get; set; }

    public bool? IsWillCall { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public ListTripsQuery ToQuery() =>
        new(
            StartDate.HasValue ? LocalDate.FromDateOnly(StartDate.Value) : null,
            EndDate.HasValue ? LocalDate.FromDateOnly(EndDate.Value) : null,
            Search,
            BrokerStatus,
            IsWillCall,
            Page,
            PageSize
        );
}
