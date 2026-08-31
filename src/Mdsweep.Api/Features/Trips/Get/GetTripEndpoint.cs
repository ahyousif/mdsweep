using Mdsweep.Api.Common.Extensions;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips;
using Mdsweep.Application.Trips.Get;
using JasperFx.MultiTenancy;

namespace Mdsweep.Api.Features.Trips.Get;

public static class GetTripEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = TenantAuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripConstants.IdRoute)]
    public static async Task<IResult> Get(
        Guid id,
        TenantId activeTenant,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.InvokeForTenantAsync<ArdalisResult.Result<TripModel>>(
            activeTenant.Value!,
            new GetTripQuery(id),
            ct
        );
        return result.ToEndpointResult(TripResponse.FromModel);
    }
}
