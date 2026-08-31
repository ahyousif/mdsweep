using Mdsweep.Application.Common.Authorization;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public sealed class TenantAccess(DbContextOptions<ApplicationDbContext> options) : ITenantAccess
{
    public async Task<IReadOnlyList<TenantMembershipContext>> GetMembershipsAsync(
        string userSubject,
        CancellationToken cancellationToken
    )
    {
        await using var db = new ApplicationDbContext(options);
        return await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            where user.KeycloakUserId == userSubject
            select new TenantMembershipContext(user.Id, membership.TenantId, membership.Role)
        ).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasRoleAsync(
        string userSubject,
        string tenantId,
        string role,
        CancellationToken cancellationToken
    )
    {
        await using var db = new ApplicationDbContext(options);
        return await (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            where
                user.KeycloakUserId == userSubject
                && membership.TenantId == tenantId
                && membership.Role == role
            select membership
        ).AnyAsync(cancellationToken);
    }
}
