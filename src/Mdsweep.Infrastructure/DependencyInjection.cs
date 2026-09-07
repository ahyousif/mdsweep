using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Infrastructure.Routing;

namespace Mdsweep.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeycloakAuthenticationOptions>()
            .Bind(configuration.GetSection(KeycloakAuthenticationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Authority), "Authentication authority is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Authentication client id is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientSecret),
                "Authentication client secret is required."
            )
            .ValidateOnStart();

        services
            .AddOptions<KeycloakAdministrationOptions>()
            .Bind(configuration.GetSection(KeycloakAdministrationOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientId),
                "Keycloak administration client id is required."
            )
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientSecret),
                "Keycloak administration client secret is required."
            )
            .ValidateOnStart();

        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<IRepository>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITenantAccess, TenantAccess>();

        services
            .AddOptions<GoogleRoutesOptions>()
            .Bind(configuration.GetSection(GoogleRoutesOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Google Routes API key is required.")
            .ValidateOnStart();

        services.AddHttpClient(
            GoogleRouteDurationProvider.HttpClientName,
            client =>
            {
                client.BaseAddress = new Uri("https://routes.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            }
        );

        services.AddScoped<IRouteDurationProvider, GoogleRouteDurationProvider>();

        services.AddHttpClient<IKeycloakUserAdministration, KeycloakUserAdministration>();

        return services;
    }
}
