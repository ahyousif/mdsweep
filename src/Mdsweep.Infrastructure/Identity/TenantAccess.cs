using Mdsweep.Application.Common.Authorization;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public sealed class TenantAccess(ApplicationDbContext db) : ITenantAccess
{
    public async Task<IReadOnlyList<TenantMembershipInfo>> GetMembershipsAsync(
        string userSubject,
        CancellationToken cancellationToken
    )
    {
        var memberships = await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            join tenant in db.Tenants on membership.TenantId equals tenant.Id
            where user.KeycloakUserId == userSubject && user.IsActive
            select new { user.Id, user.FirstName, user.LastName, membership.TenantId, membership.Roles, TenantName = tenant.Name }
        ).ToListAsync(cancellationToken);
        return memberships.SelectMany(membership => membership.Roles.Select(role => new TenantMembershipInfo(
            membership.Id, membership.FirstName, membership.LastName, membership.TenantId, membership.TenantName, role))).ToArray();
    }

    public async Task<bool> HasRoleAsync(
        string userSubject,
        string tenantId,
        string role,
        CancellationToken cancellationToken
    ) =>
        await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            where user.KeycloakUserId == userSubject && user.IsActive && membership.TenantId == tenantId && membership.Roles.Contains(role)
            select membership
        ).AnyAsync(cancellationToken);
}
