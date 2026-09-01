using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static WolverineOptions AddPersistence(this WolverineOptions options, IConfiguration configuration)
    {
        var connectionString = Guard.Against.NullOrWhiteSpace(configuration.GetConnectionString("mdsweep"));

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
