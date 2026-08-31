using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Common.Authorization;

namespace Mdsweep.Api.Features.Dispatch;

internal static class DispatchAuthorization
{
    public static async Task<TenantContext?> ResolveDispatcher(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        return TenantContextResolver.HasRole(context, "Dispatcher") ? context : null;
    }
}
