using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class DeactivateVehicleEndpoint
{
    [WolverinePost(DispatchRoutes.DeactivateVehicle)]
    public static async Task<IResult> Post(
        Guid vehicleId,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateVehicle(context.ProviderId, vehicleId),
            cancellationToken
        );
        return DispatchHttpResult.Map(result, _ => Results.NoContent());
    }
}
