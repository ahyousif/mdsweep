using Mdsweep.Domain.Common.Abstractions;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;

namespace Mdsweep.Infrastructure.Persistence;

public static class MdsweepPersistenceExtensions
{
    public static WolverineOptions AddMdsweepPersistence(this WolverineOptions options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("mdsweep");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'mdsweep' is required.");

        options.PersistMessagesWithPostgresql(connectionString);
        options
            .UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight)
            .WithDbContextAbstraction<IRepository, ApplicationDbContext>();
        options.Policies.AutoApplyTransactions();
        options.PublishDomainEventsFromEntityFrameworkCore<IAggregateRoot, DomainEvent>(aggregate =>
            aggregate.DequeueDomainEvents()
        );
        options.Services.AddDbContextWithWolverineManagedConjoinedTenancy<ApplicationDbContext>(
            (db, connectionString) =>
                db.UseNpgsql(
                        connectionString.Value,
                        npgsql =>
                            npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName).UseNodaTime()
                    )
                    // Narrowly suppresses Wolverine-managed conjoined-tenancy runtime filters,
                    // which do not change the EF migration model.
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
        );
        options.Services.AddWolverineConjoinedTenancyWorkaround();

        return options;
    }
}
