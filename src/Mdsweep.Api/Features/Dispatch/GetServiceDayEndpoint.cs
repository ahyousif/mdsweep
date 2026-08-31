using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetServiceDayEndpoint
{
    [WolverineGet("/api/service-days/{serviceDate}/trips")]
    public static async Task<IResult> Get(
        DateOnly serviceDate,
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

        var trips = await bus.InvokeAsync<List<ServiceDayTripResponse>>(
            new GetServiceDay(context.TenantId, serviceDate),
            cancellationToken
        );
        return Results.Ok(trips);
    }
}
