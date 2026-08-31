using Mdsweep.Application.Common.Authorization;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public sealed class TenantAccess(ApplicationDbContext db) : ITenantAccess
{
    public Task<bool> HasRoleAsync(
        string userSubject,
        string tenantId,
        string role,
        CancellationToken cancellationToken
    ) =>
        (
            from user in db.Users
            join membership in db.TenantMemberships on user.Id equals membership.UserId
            where
                user.KeycloakUserId == userSubject
                && membership.TenantId == tenantId
                && membership.Role == role
            select membership
        ).AnyAsync(cancellationToken);
}
