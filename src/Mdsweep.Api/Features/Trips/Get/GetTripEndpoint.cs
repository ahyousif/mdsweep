using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.Get;

namespace Mdsweep.Api.Features.Trips.Get;

public static class GetTripEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripConstants.IdRoute)]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetTripQuery(id), ct);
        return result.ToEndpointResult(TripResponse.FromModel);
    }
}
