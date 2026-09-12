using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public static class DevelopmentIdentitySeeder
{
    private const string DispatcherSubject = "d4ba70d7-6173-4ad0-9b48-59aa2c6a322a";
    private const string TenantId = "mdsw-eep2-3456";

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.FindAsync([TenantId], cancellationToken);
        if (tenant is null)
        {
            tenant = TenantAggregate.Create(TenantId, "MDSweep Tenant");
            db.Tenants.Add(tenant);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.KeycloakUserId == DispatcherSubject, cancellationToken);
        if (user is null)
        {
            user = UserAggregate.Create("MDSweep", "Developer", DispatcherSubject, "developer@mdsweep.com");
            db.Users.Add(user);
        }

        if (
            !await db.TenantMemberships.AnyAsync(x => x.UserId == user.Id && x.TenantId == tenant.Id, cancellationToken)
        )
        {
            db.TenantMemberships.Add(
                TenantMembership.Create(tenant.Id, user.Id, "MDSweep Developer", ["Administrator"])
            );
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
