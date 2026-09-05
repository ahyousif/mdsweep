using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.ResetScheduledPickupTime;

namespace Mdsweep.Api.Features.Trips.ResetScheduledPickupTime;

public static class ResetScheduledPickupTimeEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsManage)]
    [WolverinePost(TripConstants.ResetScheduledPickupTimeRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new ResetScheduledPickupTimeCommand(id), ct);
        return result.ToEndpointResult(value => Results.Ok(new { value }));
    }
}
