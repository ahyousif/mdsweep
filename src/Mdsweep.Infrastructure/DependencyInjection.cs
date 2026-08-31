using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Infrastructure.TripImports.Parsing;
using Mdsweep.Infrastructure.TripImports.Persistence;

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
        services.AddScoped<ITripImportLookup, EfTripImportLookup>();
        services.AddSingleton<ITripImportFileParser, CsvTripImportFileParser>();
        services.AddSingleton<ITripImportFileParser, XlsxTripImportFileParser>();

        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>();

        return services;
    }
}
