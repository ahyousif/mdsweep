using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Vehicles;

namespace Mdsweep.Api.Features.Vehicles.Update;

public sealed class UpdateVehicleEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverinePut(VehicleConstants.IdRoute)]
    public static async Task<IResult> Put(Guid id, UpdateVehicleRequest request, IMessageBus bus, CancellationToken ct)
    {
        try
        {
            var result = await bus.SendAsync(request.ToCommand(id), ct);
            return result.ToEndpointResult();
        }
        catch (VehicleVinConflictException)
        {
            return Result.Invalid(VehicleErrors.DuplicateVin()).ToEndpointResult();
        }
    }
}
