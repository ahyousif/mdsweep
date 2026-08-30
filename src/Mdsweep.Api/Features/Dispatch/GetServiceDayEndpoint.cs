using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetServiceDayEndpoint
{
    [WolverineGet(DispatchRoutes.ServiceDay)]
    public static async Task<IResult> Get(
        DateOnly serviceDate,
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

        var trips = await bus.InvokeAsync<List<ServiceDayTripResponse>>(
            new GetServiceDay(context.ProviderId, serviceDate),
            cancellationToken
        );
        return Results.Ok(trips);
    }
}
