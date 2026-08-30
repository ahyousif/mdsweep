using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetScheduledPickupTimeHistoryEndpoint
{
    [WolverineGet(DispatchRoutes.ScheduledPickupTimeHistory)]
    public static async Task<IResult> Get(
        string tripNumber,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, bus, cancellationToken);
        if (context is null || !ProviderContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var result = await bus.InvokeAsync<GetScheduledPickupTimeHistoryResult>(
            new GetScheduledPickupTimeHistory(context.ProviderId, tripNumber),
            cancellationToken
        );

        return result.Found ? Results.Ok(result.Changes) : Results.NotFound();
    }
}
