using Mdsweep.Infrastructure.Identity;

namespace Mdsweep.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeInfrastructureAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            await db.Database.MigrateAsync();
        }

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
