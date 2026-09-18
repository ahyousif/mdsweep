using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Passengers.Disable;

namespace Mdsweep.Api.Features.Passengers.Disable;

public sealed class DisablePassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverinePost(PassengerConstants.DisableRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new DisablePassengerCommand(id), ct);
        return result.ToEndpointResult();
    }
}
