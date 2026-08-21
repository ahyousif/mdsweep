using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Features.ManifestImports;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.Identity;

namespace Mdsweep.Api.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripBrokerImport> TripBrokerImports => Set<TripBrokerImport>();
    public DbSet<ManifestPreview> ManifestPreviews => Set<ManifestPreview>();
    public DbSet<TripSchedule> TripSchedules => Set<TripSchedule>();
    public DbSet<ScheduledPickupTimeChange> ScheduledPickupTimeChanges => Set<ScheduledPickupTimeChange>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TripAssignment> TripAssignments => Set<TripAssignment>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ProviderMembership> ProviderMemberships => Set<ProviderMembership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
