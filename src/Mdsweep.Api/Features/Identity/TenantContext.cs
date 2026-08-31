using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;

namespace Mdsweep.Api.Features.Identity;

/// <summary>The active Tenant membership for an authenticated HTTP request.</summary>
public sealed record TenantContext(string TenantId, Guid UserId, string Role);

public static class TenantContextResolver
{
    public static bool HasRole(TenantContext? context, string role) =>
        context is not null && string.Equals(context.Role, role, StringComparison.Ordinal);

    public static async Task<TenantContext?> ResolveActive(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        CancellationToken cancellationToken
    )
    {
        var userSubject = user.FindFirstValue("sub");
        var tenantId = user.FindFirstValue(TenantClaimTypes.ActiveTenantId);
        if (string.IsNullOrWhiteSpace(userSubject) || string.IsNullOrWhiteSpace(tenantId))
            return null;

        var membership = (await tenantAccess.GetMembershipsAsync(userSubject, cancellationToken))
            .SingleOrDefault(x => x.TenantId == tenantId);

        return membership is null
            ? null
            : new TenantContext(membership.TenantId, membership.UserId, membership.Role);
    }
}
