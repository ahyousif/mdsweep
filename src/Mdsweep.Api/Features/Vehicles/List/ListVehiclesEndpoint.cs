using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Vehicles.List;

namespace Mdsweep.Api.Features.Vehicles.List;

public sealed class ListVehiclesEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverineGet(VehicleConstants.Route)]
    public static async Task<IResult> Get(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new ListVehiclesQuery(), ct);
        return result.ToEndpointResult(models => models.Select(VehicleResponse.FromModel).ToList());
    }
}
