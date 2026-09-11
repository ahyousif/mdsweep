using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Infrastructure;
using Mdsweep.Infrastructure.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mdsweep.Api.IntegrationTests;

public sealed class GoogleRoutesConfigurationTests
{
    [Fact]
    public async Task Infrastructure_starts_with_a_supplied_Google_Routes_key()
    {
        using var host = CreateHost(apiKey: "synthetic-google-routes-api-key");

        await host.StartAsync();

        using var scope = host.Services.CreateScope();
        Assert.IsType<GoogleRouteEstimateProvider>(scope.ServiceProvider.GetRequiredService<IRouteEstimateProvider>());
    }

    [Fact]
    public async Task Infrastructure_fails_to_start_when_the_Google_Routes_key_is_missing()
    {
        using var host = CreateHost(apiKey: null);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains("Google Routes API key is required.", exception.Message);
    }

    private static IHost CreateHost(string? apiKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = "https://keycloak.test/realms/mdsweep",
            ["Authentication:ClientId"] = "mdsweep-test",
            ["Authentication:ClientSecret"] = "test-secret",
            ["Web:BaseUrl"] = "https://web.mdsweep.test",
            ["KeycloakAdministration:ClientId"] = "mdsweep-administration-test",
            ["KeycloakAdministration:ClientSecret"] = "test-administration-secret",
            ["GoogleRoutes:ApiKey"] = apiKey,
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder.Build();
    }
}
