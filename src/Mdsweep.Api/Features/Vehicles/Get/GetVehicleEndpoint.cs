using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Vehicles.Get;

namespace Mdsweep.Api.Features.Vehicles.Get;

public sealed class GetVehicleEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverineGet(VehicleConstants.IdRoute)]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetVehicleQuery(id), ct);
        return result.ToEndpointResult(VehicleResponse.FromModel);
    }
}
