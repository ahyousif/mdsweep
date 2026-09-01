using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Common.Pagination;
using Mdsweep.Application.Trips.List;
using NodaTime.Text;

namespace Mdsweep.Api.Features.Trips.List;

public static class ListTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripConstants.Route)]
    public static async Task<IResult> Get(
        IMessageBus bus,
        CancellationToken ct,
        string? serviceDate = null,
        string? brokerStatus = null,
        bool? isWillCall = null,
        int page = 1,
        int pageSize = 50,
        TripSortBy sortBy = TripSortBy.AppointmentTime,
        SortDirection sortDirection = SortDirection.Ascending
    )
    {
        LocalDate? parsedServiceDate = null;
        if (serviceDate is not null)
        {
            var parsed = LocalDatePattern.Iso.Parse(serviceDate);
            if (!parsed.Success)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["serviceDate"] = ["Service date must use ISO-8601 format (yyyy-MM-dd)."],
                    }
                );
            }

            parsedServiceDate = parsed.Value;
        }

        var result = await bus.SendAsync(
            new ListTripsQuery(parsedServiceDate, brokerStatus, isWillCall, page, pageSize, sortBy, sortDirection),
            ct
        );
        return result.ToEndpointResult(pageResult => new PagedTripsResponse(
            pageResult.Items.Select(TripResponse.FromModel).ToList(),
            pageResult.TotalCount,
            pageResult.Page,
            pageResult.PageSize
        ));
    }

    private sealed record PagedTripsResponse(IReadOnlyList<TripResponse> Items, int TotalCount, int Page, int PageSize);
}
