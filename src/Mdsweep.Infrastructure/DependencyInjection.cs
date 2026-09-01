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
        services.AddOptions<KeycloakAuthenticationOptions>()
            .Bind(configuration.GetSection(KeycloakAuthenticationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Authority), "Authentication authority is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Authentication client id is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret), "Authentication client secret is required.")
            .ValidateOnStart();
        services.AddOptions<KeycloakAdministrationOptions>()
            .Bind(configuration.GetSection(KeycloakAdministrationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Keycloak administration client id is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret), "Keycloak administration client secret is required.")
            .ValidateOnStart();
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<IRepository>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITenantAccess, TenantAccess>();
        services.AddScoped<ITripImportLookup, EfTripImportLookup>();
        services.AddSingleton<ITripImportFileParser, CsvTripImportFileParser>();
        services.AddSingleton<ITripImportFileParser, XlsxTripImportFileParser>();

        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>();

        return services;
    }
}
