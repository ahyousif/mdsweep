using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class GetAssignmentHistoryEndpoint
{
    [WolverineGet(DispatchRoutes.AssignmentHistory)]
    public static async Task<IResult> Get(
        string tripNumber,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<
            DispatchManagementResult<IReadOnlyList<AssignmentResponse>>
        >(new GetAssignmentHistory(tripNumber), cancellationToken);
        return DispatchHttpResult.Map(result, value => Results.Ok(value.Value));
    }
}
