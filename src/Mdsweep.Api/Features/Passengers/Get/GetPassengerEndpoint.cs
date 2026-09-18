using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Passengers.Get;

namespace Mdsweep.Api.Features.Passengers.Get;

public static class GetPassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverineGet(PassengerConstants.IdRoute)]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetPassengerQuery(id), ct);

        return result.ToEndpointResult(PassengerResponse.FromModel);
    }
}
