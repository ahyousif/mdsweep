using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class SetScheduledPickupTimeEndpoint
{
    [WolverinePut(DispatchRoutes.ScheduledPickupTime)]
    public static async Task<IResult> Put(
        string tripNumber,
        SetScheduledPickupTimeRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<SetScheduledPickupTimeResult>(
            new SetScheduledPickupTime(context.UserId, tripNumber, request.ScheduledPickupTime),
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
