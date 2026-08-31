using System.Security.Claims;
using Mdsweep.Application.Common.Authorization;

namespace Mdsweep.Api.Features.Identity;

public static class TenantAuthorizationPolicies
{
    public const string Dispatcher = "tenant-dispatcher";
}

public sealed record TenantRoleRequirement(string Role) : IAuthorizationRequirement;

public sealed class TenantRoleAuthorizationHandler(ITenantAccess tenantAccess)
    : AuthorizationHandler<TenantRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRoleRequirement requirement
    )
    {
        var userSubject = context.User.FindFirstValue("sub");
        var tenantId = context.User.FindFirstValue(TenantClaimTypes.ActiveTenantId);
        if (string.IsNullOrWhiteSpace(userSubject) || string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        if (await tenantAccess.HasRoleAsync(userSubject, tenantId, requirement.Role, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
