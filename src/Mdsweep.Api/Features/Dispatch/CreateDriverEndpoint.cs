using System.Security.Claims;
using Mdsweep.Application.Dispatch;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class CreateDriverEndpoint
{
    [WolverinePost(DispatchRoutes.Drivers)]
    public static async Task<IResult> Post(
        CreateDriverRequest request,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatchAuthorization.ResolveDispatcher(user, bus, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriver(context.ProviderId, request),
            cancellationToken
        );
        return DispatchHttpResult.Map(
            result,
            value => Results.Created(value.Location!, value.Value)
        );
    }
}
