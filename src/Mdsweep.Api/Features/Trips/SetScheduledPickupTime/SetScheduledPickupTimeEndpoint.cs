using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.SetScheduledPickupTime;

namespace Mdsweep.Api.Features.Trips.SetScheduledPickupTime;

public static class SetScheduledPickupTimeEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverinePut(TripConstants.ScheduledPickupTimeRoute)]
    public static async Task<IResult> Put(
        Guid id,
        SetScheduledPickupTimeRequest req,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var result = await bus.SendAsync(new SetScheduledPickupTimeCommand(id, req.ScheduledPickupTime), ct);

        return result.ToEndpointResult(value => Results.Ok(new { value }));
    }
}
