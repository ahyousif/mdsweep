using System.Security.Claims;
using Mdsweep.Api.Common.Authentication;

namespace Mdsweep.Api.Common.Authorization;

public sealed class TenantRoleAuthorizationHandler(ITenantAccess tenantAccess)
    : AuthorizationHandler<TenantRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRoleRequirement requirement
    )
    {
        var userSubject = context.User.FindFirstValue("sub");
        var tenantId = context.User.FindFirstValue(CustomClaimTypes.ActiveTenantId);

        if (string.IsNullOrWhiteSpace(userSubject) || string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        var hasRole = await tenantAccess.HasRoleAsync(userSubject, tenantId, requirement.Role, CancellationToken.None);

        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }
}
