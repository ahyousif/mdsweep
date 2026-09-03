using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Identity;

public static class DevelopmentIdentitySeeder
{
    private const string DispatcherSubject = "d4ba70d7-6173-4ad0-9b48-59aa2c6a322a";
    private const string TenantOrganizationId = "b6d8ea17-b31c-45c0-b5f2-4fec5df7c6cf";

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(
            x => x.KeycloakOrganizationId == TenantOrganizationId,
            cancellationToken
        );
        if (tenant is null)
        {
            tenant = TenantAggregate.Create("mdsw-eep2-3456", "Synthetic Tenant", TenantOrganizationId);
            db.Tenants.Add(tenant);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.KeycloakUserId == DispatcherSubject, cancellationToken);
        if (user is null)
        {
            user = UserAggregate.Create("Synthetic", "Dispatcher", DispatcherSubject);
            db.Users.Add(user);
        }

        if (
            !await db.TenantMemberships.AnyAsync(
                x => x.TenantId == tenant.Id && x.UserId == user.Id && x.Role == "Dispatcher",
                cancellationToken
            )
        )
        {
            db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, user.Id, "Dispatcher"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
