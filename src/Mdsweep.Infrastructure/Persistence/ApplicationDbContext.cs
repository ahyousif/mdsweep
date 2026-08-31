using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.Identity;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options),
        IRepository
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripBrokerImport> TripBrokerImports => Set<TripBrokerImport>();
    public DbSet<ManifestPreview> ManifestPreviews => Set<ManifestPreview>();
    public DbSet<TripSchedule> TripSchedules => Set<TripSchedule>();
    public DbSet<ScheduledPickupTimeChange> ScheduledPickupTimeChanges => Set<ScheduledPickupTimeChange>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TripAssignment> TripAssignments => Set<TripAssignment>();
    public DbSet<DriverTripEvent> DriverTripEvents => Set<DriverTripEvent>();
    public DbSet<DriverTripEventCorrection> DriverTripEventCorrections => Set<DriverTripEventCorrection>();
    public DbSet<DriverTripSyncConflict> DriverTripSyncConflicts => Set<DriverTripSyncConflict>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ProviderMembership> ProviderMemberships => Set<ProviderMembership>();
    public DbSet<PassengerAggregate> Passengers => Set<PassengerAggregate>();
    public DbSet<TenantAggregate> Tenants => Set<TenantAggregate>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<UserAggregate> Users => Set<UserAggregate>();

    public Task<TAggregate?> GetByIdAsync<TAggregate, TId>(TId id, CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull => Set<TAggregate>().FindAsync([id], ct).AsTask();

    async Task IRepository.AddAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
    {
        await Set<TAggregate>().AddAsync(aggregate, ct);
    }

    public Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : AggregateRoot<Guid>
    {
        Set<TAggregate>().Update(aggregate);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : AggregateRoot<Guid>
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
