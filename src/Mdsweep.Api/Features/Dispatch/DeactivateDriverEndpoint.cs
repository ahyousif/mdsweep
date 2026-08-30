using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class DeactivateDriverEndpoint
{
    [WolverinePost(DispatchRoutes.DeactivateDriver)]
    public static async Task<IResult> Post(
        Guid driverId,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateDriver(context.ProviderId, driverId),
            cancellationToken
        );
        return DispatchHttpResult.Map(result, _ => Results.NoContent());
    }
}
