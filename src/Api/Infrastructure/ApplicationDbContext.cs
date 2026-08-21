using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Features.ManifestImports;
using Mdsweep.Api.Features.Dispatch;

namespace Mdsweep.Api.Infrastructure;

public sealed class ApplicationUser : IdentityUser;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripBrokerImport> TripBrokerImports => Set<TripBrokerImport>();
    public DbSet<ManifestPreview> ManifestPreviews => Set<ManifestPreview>();
    public DbSet<TripSchedule> TripSchedules => Set<TripSchedule>();
    public DbSet<ScheduledPickupTimeChange> ScheduledPickupTimeChanges => Set<ScheduledPickupTimeChange>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
