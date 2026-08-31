using Mdsweep.Application.DriverWork;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Infrastructure.DriverWork;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMdsweepInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<IRepository>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITenantAccess, TenantAccess>();

        services.AddSingleton<IDriverWorkClock, SystemDriverWorkClock>();
        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>();

        return services;
    }
}
