using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Features.ManifestImports;

namespace Mdsweep.Api.Infrastructure;

public sealed class ApplicationUser : IdentityUser;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ManifestPreview> ManifestPreviews => Set<ManifestPreview>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
