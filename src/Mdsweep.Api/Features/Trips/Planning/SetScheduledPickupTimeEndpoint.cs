using Mdsweep.Api.Common.Extensions;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.Planning;
using JasperFx.MultiTenancy;

namespace Mdsweep.Api.Features.Trips.Planning;

public static class SetScheduledPickupTimeEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = TenantAuthorizationPolicies.Dispatcher)]
    [WolverinePut(TripConstants.ScheduledPickupTimeRoute)]
    public static async Task<IResult> Put(
        Guid id,
        SetScheduledPickupTimeRequest request,
        TenantId activeTenant,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.InvokeForTenantAsync<ArdalisResult.Result<SetScheduledPickupTimeResult>>(
            activeTenant.Value!,
            new SetScheduledPickupTime(id, request.ScheduledPickupTime),
            ct
        );
        return result.ToEndpointResult(value => Results.Ok(new { value.ScheduledPickupTime }));
    }
}
