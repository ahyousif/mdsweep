using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options),
        IRepository
{
    public DbSet<TripAggregate> Trips => Set<TripAggregate>();
    public DbSet<TripImportAggregate> TripImports => Set<TripImportAggregate>();
    public DbSet<PassengerAggregate> Passengers => Set<PassengerAggregate>();
    public DbSet<TenantAggregate> Tenants => Set<TenantAggregate>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<UserAggregate> Users => Set<UserAggregate>();

    public Task<TAggregate?> GetByIdAsync<TAggregate, TId>(TId id, CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull => Set<TAggregate>().FindAsync([id], ct).AsTask();

    public Task<TAggregate?> SingleOrDefaultAsync<TAggregate>(
        ISpecification<TAggregate> specification,
        CancellationToken ct
    )
        where TAggregate : class, IAggregateRoot
    {
        return SpecificationEvaluator
            .Default.GetQuery(Set<TAggregate>().AsQueryable(), specification)
            .SingleOrDefaultAsync(ct);
    }

    public Task<TResult?> SingleOrDefaultAsync<TAggregate, TResult>(
        ISpecification<TAggregate, TResult> specification,
        CancellationToken ct
    )
        where TAggregate : class, IAggregateRoot
    {
        return SpecificationEvaluator
            .Default.GetQuery(Set<TAggregate>().AsQueryable(), specification)
            .SingleOrDefaultAsync(ct);
    }

    public Task<List<TResult>> ListAsync<TAggregate, TResult>(
        ISpecification<TAggregate, TResult> specification,
        CancellationToken ct
    )
        where TAggregate : class, IAggregateRoot =>
        SpecificationEvaluator.Default.GetQuery(Set<TAggregate>().AsQueryable(), specification).ToListAsync(ct);

    public Task<int> CountAsync<TAggregate>(ISpecification<TAggregate> specification, CancellationToken ct)
        where TAggregate : class, IAggregateRoot =>
        SpecificationEvaluator.Default.GetQuery(Set<TAggregate>().AsQueryable(), specification).CountAsync(ct);

    async Task IRepository.AddAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
    {
        await Set<TAggregate>().AddAsync(aggregate, ct);
    }

    public Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : class, IAggregateRoot
    {
        Set<TAggregate>().Update(aggregate);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : class, IAggregateRoot
    {
        Set<TAggregate>().Remove(aggregate);
        return Task.CompletedTask;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
