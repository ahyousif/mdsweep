using System.Security.Claims;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Identity;

public static class GetCurrentUserEndpoint
{
    [WolverineGet(IdentityRoutes.CurrentUser)]
    public static async Task<IResult> Get(
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var contexts = await ProviderContextResolver.ResolveAll(user, bus, cancellationToken);
        return contexts.Count == 0
            ? Results.Forbid()
            : Results.Ok(
                contexts.Select(context => new
                {
                    appUserId = context.AppUserId,
                    providerId = context.ProviderId,
                    role = context.Role,
                })
            );
    }
}
