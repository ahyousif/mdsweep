namespace Mdsweep.Infrastructure.Persistence;

public static class ApplicationDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder<ApplicationDbContext> ConfigureForMdsweep(
        this DbContextOptionsBuilder<ApplicationDbContext> options,
        string connectionString
    )
    {
        return options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName).UseNodaTime()
            )
            .UseSnakeCaseNamingConvention();
    }
}
