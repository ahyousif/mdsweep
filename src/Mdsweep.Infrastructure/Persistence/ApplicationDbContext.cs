using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Vehicles;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Trips;
using Mdsweep.Domain.Users;
using Mdsweep.Domain.Vehicles;
using Mdsweep.Infrastructure.Persistence.Configuration;
using Npgsql;

namespace Mdsweep.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options),
        IRepository
{
    public DbSet<TripAggregate> Trips => Set<TripAggregate>();
    public DbSet<JourneyAggregate> Journeys => Set<JourneyAggregate>();
    public DbSet<PassengerAggregate> Passengers => Set<PassengerAggregate>();
    public DbSet<TenantAggregate> Tenants => Set<TenantAggregate>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<UserAggregate> Users => Set<UserAggregate>();
    public DbSet<InvitationAggregate> Invitations => Set<InvitationAggregate>();
    public DbSet<VehicleAggregate> Vehicles => Set<VehicleAggregate>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                    is PostgresException
                    {
                        SqlState: PostgresErrorCodes.UniqueViolation,
                        ConstraintName: VehicleConfiguration.VinUniqueIndex
                    }
            )
        {
            throw new VehicleVinConflictException(exception);
        }
    }

    // Single
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

    // List

    public Task<List<TResult>> ListAsync<TAggregate, TResult>(
        ISpecification<TAggregate, TResult> specification,
        CancellationToken ct
    )
        where TAggregate : class, IAggregateRoot =>
        SpecificationEvaluator.Default.GetQuery(Set<TAggregate>().AsQueryable(), specification).ToListAsync(ct);

    public Task<List<TAggregate>> ListAsync<TAggregate>(ISpecification<TAggregate> specification, CancellationToken ct)
        where TAggregate : class, IAggregateRoot =>
        SpecificationEvaluator.Default.GetQuery(Set<TAggregate>().AsQueryable(), specification).ToListAsync(ct);

    // Count
    public Task<int> CountAsync<TAggregate>(ISpecification<TAggregate> specification, CancellationToken ct)
        where TAggregate : class, IAggregateRoot =>
        SpecificationEvaluator
            .Default.GetQuery(Set<TAggregate>().AsQueryable(), specification, evaluateCriteriaOnly: true)
            .CountAsync(ct);

    // Write
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
