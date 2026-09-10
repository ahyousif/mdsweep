using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Mdsweep.Api.Features.Trips.List;

public static class ListTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsViewAll)]
    [WolverineGet(TripConstants.Route)]
    public static async Task<IResult> Get([FromQuery] ListTripsRequest req, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(req.ToQuery(), ct);

        return result.ToEndpointResult(value => new
        {
            Items = value.Items.Select(TripResponse.FromModel),
            value.TotalCount,
            value.Page,
            value.PageSize,
            value.TotalPages,
        });
    }
}
