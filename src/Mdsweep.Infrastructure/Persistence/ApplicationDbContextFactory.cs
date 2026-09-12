namespace Mdsweep.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        options.ConfigureForMdsweep("Host=localhost;Database=mdsweep_design;Username=postgres;Password=postgres");

        return new ApplicationDbContext(options.Options);
    }
}
