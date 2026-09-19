using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Vehicles.Update;

public sealed class UpdateVehicleEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverinePut(VehicleConstants.IdRoute)]
    public static async Task<IResult> Put(Guid id, UpdateVehicleRequest request, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(request.ToCommand(id), ct);
        return result.ToEndpointResult();
    }
}
