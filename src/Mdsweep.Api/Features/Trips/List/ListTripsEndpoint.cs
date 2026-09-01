using Mdsweep.Api.Common.Authorization;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Trips.List;

public static class ListTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripConstants.Route)]
    public static async Task<ArdalisResult.IResult> Get(
        [AsParameters] ListTripsRequest request,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.SendAsync(request.ToQuery(), ct);

        return result;
    }
}
