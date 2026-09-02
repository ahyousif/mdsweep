using Mdsweep.Infrastructure.Identity;

namespace Mdsweep.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeInfrastructureAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // TODO: migrations should be applied in a separate step, not on app startup. This is a temporary measure to avoid having to run migrations manually during development.
        await db.Database.MigrateAsync();

        // Managed conjoined tenancy persists its tenant registry in Wolverine storage.
        // Provision registered Wolverine resources before registering application tenants.
        await JasperFx.Resources.ResourceHostExtensions.SetupResources(host);

        if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            await DevelopmentIdentitySeeder.SeedAsync(db);
        }

        var tenantIds = await db.Tenants.Select(tenant => tenant.Id).ToArrayAsync();

        await host.AddWolverineManagedTenantsAsync<ApplicationDbContext>(tenantIds);
    }
}
