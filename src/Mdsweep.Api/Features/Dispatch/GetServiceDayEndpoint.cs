using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
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
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var trips = await bus.InvokeAsync<List<ServiceDayTripResponse>>(
            new GetServiceDay(serviceDate),
            cancellationToken
        );
        return Results.Ok(trips);
    }
}
