using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Identity;
using Wolverine;

namespace Mdsweep.Api.Features.Dispatch;

internal static class DispatchAuthorization
{
    public static async Task<ProviderContext?> ResolveDispatcher(
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, bus, cancellationToken);
        return ProviderContextResolver.HasRole(context, "Dispatcher") ? context : null;
    }
}
