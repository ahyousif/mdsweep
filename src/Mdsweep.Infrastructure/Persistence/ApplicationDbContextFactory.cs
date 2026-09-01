namespace Mdsweep.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();

        options.UseNpgsql(
            "Host=localhost;Database=mdsweep_design;Username=postgres;Password=postgres",
            npgsql => npgsql.UseNodaTime()
        );

        return new ApplicationDbContext(options.Options);
    }
}
