using Mdsweep.Application.DriverWork;
using Mdsweep.Infrastructure.DriverWork;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mdsweep.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMdsweepInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("mdsweep"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            )
        );

        services.AddSingleton<IDriverWorkClock, SystemDriverWorkClock>();
        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>();

        return services;
    }
}
