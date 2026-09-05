using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.SetScheduledPickupTime;

namespace Mdsweep.Api.Features.Trips.SetScheduledPickupTime;

public static class SetScheduledPickupTimeEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsManage)]
    [WolverinePut(TripConstants.ScheduledPickupTimeRoute)]
    public static async Task<IResult> Put(
        Guid id,
        SetScheduledPickupTimeRequest req,
        IMessageBus bus,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var result = await bus.SendAsync(new SetScheduledPickupTimeCommand(id, req.ScheduledPickupTime), ct);

        return result.ToEndpointResult(value => Results.Ok(new { value }));
    }
}
