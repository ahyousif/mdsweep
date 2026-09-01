using Microsoft.Extensions.Hosting;
using Mdsweep.Infrastructure.Identity;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Mdsweep.Infrastructure.Persistence;

public static class MdsweepInfrastructureInitializationExtensions
{
    public static async Task InitializeMdsweepInfrastructureAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
            await DevelopmentIdentitySeeder.SeedAsync(db);

        var tenantIds = await db.Tenants.Select(tenant => tenant.Id).ToArrayAsync();
        await host.AddWolverineManagedTenantsAsync<ApplicationDbContext>(tenantIds);
    }
}
