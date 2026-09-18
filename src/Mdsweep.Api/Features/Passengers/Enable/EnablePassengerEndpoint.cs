using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Passengers.Enable;

namespace Mdsweep.Api.Features.Passengers.Enable;

public sealed class EnablePassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverinePost(PassengerConstants.EnableRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new EnablePassengerCommand(id), ct);
        return result.ToEndpointResult();
    }
}
