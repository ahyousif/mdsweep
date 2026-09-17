using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Vehicles.Get;

namespace Mdsweep.Api.Features.Vehicles.Create;

public sealed class CreateVehicleEndpoint
{
    [Tags(VehicleConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.VehiclesManage)]
    [WolverinePost(VehicleConstants.Route)]
    public static Task<IResult> Post(CreateVehicleRequest request, IMessageBus bus, CancellationToken ct) =>
        VehicleWriteResult.Execute(async () =>
        {
            var result = await bus.SendAsync(request.ToCommand(), ct);
            return await result.ToEndpointResultAsync(async id =>
            {
                var created = await bus.SendAsync(new GetVehicleQuery(id), ct);
                return created.ToEndpointResult(model =>
                    Results.Created($"/api/vehicles/{model.Id}", VehicleResponse.FromModel(model))
                );
            });
        });
}
