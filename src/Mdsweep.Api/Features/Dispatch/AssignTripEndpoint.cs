using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class AssignTripEndpoint
{
    [WolverinePost(DispatchRoutes.AssignTrip)]
    public static async Task<IResult> Post(
        string tripNumber,
        AssignTripRequest request,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<AssignmentMutationResponse>>(
            new AssignSingleTrip(context.ProviderId, context.AppUserId, tripNumber, request),
            cancellationToken
        );
        return DispatchHttpResult.Map(result, value => Results.Ok(value.Value));
    }
}
