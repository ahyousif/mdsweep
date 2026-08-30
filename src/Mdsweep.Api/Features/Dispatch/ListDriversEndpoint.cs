using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class ListDriversEndpoint
{
    [WolverineGet(DispatchRoutes.Drivers)]
    public static async Task<IResult> Get(
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        return Results.Ok(
            await bus.InvokeAsync<List<DriverResponse>>(
                new ListDrivers(context.ProviderId),
                cancellationToken
            )
        );
    }
}
