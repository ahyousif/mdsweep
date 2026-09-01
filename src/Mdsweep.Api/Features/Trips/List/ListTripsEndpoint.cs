using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Api.Common.Pagination;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Trips.List;

public static class ListTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripConstants.Route)]
    public static async Task<IResult> Get(
        [AsParameters] ListTripsRequest request,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.SendAsync(request.ToQuery(), ct);

        return result.ToEndpointResult(page => page.ToResponse(TripResponse.FromModel));
    }
}
