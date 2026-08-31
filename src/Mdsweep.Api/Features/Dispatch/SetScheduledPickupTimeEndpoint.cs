using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;

namespace Mdsweep.Api.Features.Dispatch;

public static class SetScheduledPickupTimeEndpoint
{
    [WolverinePut("/api/trips/{tripNumber}/scheduled-pickup-time")]
    public static async Task<IResult> Put(
        string tripNumber,
        SetScheduledPickupTimeRequest request,
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

        var result = await bus.InvokeAsync<SetScheduledPickupTimeResult>(
            new SetScheduledPickupTime(
                context.TenantId,
                context.UserId,
                tripNumber,
                request.ScheduledPickupTime
            ),
            cancellationToken
        );

        return result.Outcome switch
        {
            SetScheduledPickupTimeOutcome.Updated => Results.Ok(new { result.ScheduledPickupTime }),
            SetScheduledPickupTimeOutcome.NotFound => Results.NotFound(),
            _ => Results.BadRequest(new { message = "An inactive Trip cannot be scheduled." }),
        };
    }
}
