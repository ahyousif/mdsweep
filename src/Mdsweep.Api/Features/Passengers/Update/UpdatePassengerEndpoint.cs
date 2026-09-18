using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Passengers.Update;

public sealed class UpdatePassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverinePut(PassengerConstants.IdRoute)]
    public static async Task<IResult> Put(Guid id, UpdatePassengerRequest request, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(request.ToCommand(id), ct);
        return result.ToEndpointResult();
    }
}
