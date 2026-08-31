using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetScheduledPickupTimeHistoryEndpoint
{
    [WolverineGet("/api/trips/{tripNumber}/scheduled-pickup-time/history")]
    public static async Task<IResult> Get(
        string tripNumber,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
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
