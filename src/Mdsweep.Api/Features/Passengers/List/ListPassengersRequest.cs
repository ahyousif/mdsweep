using Mdsweep.Application.Passengers.List;

namespace Mdsweep.Api.Features.Passengers.List;

public sealed class ListPassengersRequest
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public ListPassengersQuery ToQuery() => new(Search, Page, PageSize);
}
