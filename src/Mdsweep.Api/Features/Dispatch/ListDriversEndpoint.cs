using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class ListDriversEndpoint
{
    [WolverineGet(DispatchRoutes.Drivers)]
    public static async Task<IResult> Get(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        return Results.Ok(
            await bus.InvokeAsync<List<DriverResponse>>(
                new ListDrivers(),
                cancellationToken
            )
        );
    }
}
