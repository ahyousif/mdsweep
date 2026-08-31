using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class CreateDriverAccessEndpoint
{
    [WolverinePost(DispatchRoutes.DriverAccess)]
    public static async Task<IResult> Post(
        CreateDriverAccessRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriverAccess(request),
            cancellationToken
        );
        return DispatchHttpResult.Map(
            result,
            value => Results.Created(value.Location!, value.Value)
        );
    }
}
