using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Mdsweep.Application.TripImports.Preview;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Infrastructure.Identity;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;

namespace Mdsweep.Infrastructure.Persistence;

public static class MdsweepInfrastructureHostExtensions
{
    public static IHostBuilder AddMdsweepMessaging(
        this IHostBuilder host,
        IConfiguration configuration,
        Assembly applicationAssembly
    )
    {
        host.UseWolverine(options =>
        {
            // UseWolverine captures its caller for discovery. This extension lives in
            // Infrastructure, while HTTP endpoints live in the API composition assembly.
            options.ApplicationAssembly = applicationAssembly;
            options.Discovery.IncludeAssembly(typeof(PreviewTripImportCommand).Assembly);
            options.PersistMessagesWithPostgresql(configuration.GetConnectionString("mdsweep")!);
            options
                .UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight)
                .WithDbContextAbstraction<IRepository, ApplicationDbContext>();
            options.Policies.AutoApplyTransactions();
            options.PublishDomainEventsFromEntityFrameworkCore<IAggregateRoot, DomainEvent>(aggregate =>
                aggregate.DequeueDomainEvents());
            options.Services.AddDbContextWithWolverineManagedConjoinedTenancy<ApplicationDbContext>(
                (db, connectionString) => db.UseNpgsql(connectionString.Value,
                    npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName).UseNodaTime())
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
            options.Services.AddWolverineConjoinedTenancyWorkaround();
        });

        return host;
    }

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
