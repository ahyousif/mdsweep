using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
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
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new ResetDriverAccess(driverId, request),
            cancellationToken
        );
        return DispatchHttpResult.Map(result, _ => Results.NoContent());
    }
}
