using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class CreateVehicleEndpoint
{
    [WolverinePost(DispatchRoutes.Vehicles)]
    public static async Task<IResult> Post(
        CreateVehicleRequest request,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<VehicleResponse>>(
            new CreateVehicle(context.ProviderId, request),
            cancellationToken
        );
        return DispatchHttpResult.Map(
            result,
            value => Results.Created(value.Location!, value.Value)
        );
    }
}
