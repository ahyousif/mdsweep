using Mdsweep.Application.Common.Authorization;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public sealed class TenantAccess(ApplicationDbContext db) : ITenantAccess
{
    public async Task<IReadOnlyList<TenantMembershipInfo>> GetMembershipsAsync(
        string userSubject,
        CancellationToken cancellationToken
    ) =>
        await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            join tenant in db.Tenants on membership.TenantId equals tenant.Id
            where user.KeycloakUserId == userSubject
            select new TenantMembershipInfo(
                user.Id,
                user.FirstName,
                user.LastName,
                membership.TenantId,
                tenant.Name,
                membership.Role
            )
        ).ToListAsync(cancellationToken);

    public async Task<bool> HasRoleAsync(
        string userSubject,
        string tenantId,
        string role,
        CancellationToken cancellationToken
    ) =>
        await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            where user.KeycloakUserId == userSubject && membership.TenantId == tenantId && membership.Role == role
            select membership
        ).AnyAsync(cancellationToken);
}
