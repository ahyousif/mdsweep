using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Vehicles.SetActive;

public sealed class SetVehicleActiveEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverinePut(VehicleConstants.IdRoute + "/active")]
    public static async Task<IResult> Put(
        Guid id,
        SetVehicleActiveRequest request,
        IMessageBus bus,
        CancellationToken ct
    ) => (await bus.SendAsync(request.ToCommand(id), ct)).ToEndpointResult();
}
