using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class ResetDriverAccessEndpoint
{
    [WolverinePost(DispatchRoutes.ResetDriverAccess)]
    public static async Task<IResult> Post(
        Guid driverId,
        ResetDriverAccessRequest request,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new ResetDriverAccess(context.ProviderId, driverId, request),
            cancellationToken
        );
        return DispatchHttpResult.Map(result, _ => Results.NoContent());
    }
}
