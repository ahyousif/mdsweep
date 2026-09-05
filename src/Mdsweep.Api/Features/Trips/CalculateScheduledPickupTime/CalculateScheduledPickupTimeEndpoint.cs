using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Trips.Scheduling;

namespace Mdsweep.Api.Features.Trips.CalculateScheduledPickupTime;

public static class CalculateScheduledPickupTimeEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsManage)]
    [WolverinePost(TripConstants.CalculateScheduledPickupTimeRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new CalculateScheduledPickupTimeCommand(id), ct);
        return result.ToEndpointResult(value => Results.Ok(new { value }));
    }
}
