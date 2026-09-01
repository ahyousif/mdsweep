using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Mdsweep.Domain.Common.Abstractions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;

namespace Mdsweep.Infrastructure.Persistence;

public static class MdsweepPersistenceExtensions
{
    public static WolverineOptions AddMdsweepPersistence(
        this WolverineOptions options,
        IConfiguration configuration
    )
    {
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

        return options;
    }
}
