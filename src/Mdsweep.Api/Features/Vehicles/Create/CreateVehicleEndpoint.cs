using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Vehicles;

namespace Mdsweep.Api.Features.Vehicles.Create;

public sealed class CreateVehicleEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverinePost(VehicleConstants.Route)]
    public static async Task<IResult> Post(CreateVehicleRequest request, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(request.ToCommand(), ct);
        return result.ToEndpointResult(CreatedResponse);
    }

    private static IResult CreatedResponse(VehicleModel vehicle)
    {
        return Results.Created($"/api/vehicles/{vehicle.Id}", vehicle);
    }
}
