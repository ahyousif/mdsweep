using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
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
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, bus, cancellationToken);
        if (context is null || !ProviderContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var result = await bus.InvokeAsync<SetScheduledPickupTimeResult>(
            new SetScheduledPickupTime(
                context.ProviderId,
                context.AppUserId,
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
