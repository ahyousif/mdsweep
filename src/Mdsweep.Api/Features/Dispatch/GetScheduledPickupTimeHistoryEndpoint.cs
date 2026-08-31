using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetScheduledPickupTimeHistoryEndpoint
{
    [WolverineGet("/api/trips/{tripNumber}/scheduled-pickup-time/history")]
    public static async Task<IResult> Get(
        string tripNumber,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        if (context is null || !TenantContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var result = await bus.InvokeAsync<GetScheduledPickupTimeHistoryResult>(
            new GetScheduledPickupTimeHistory(context.TenantId, tripNumber),
            cancellationToken
        );

        return result.Found ? Results.Ok(result.Changes) : Results.NotFound();
    }
}
