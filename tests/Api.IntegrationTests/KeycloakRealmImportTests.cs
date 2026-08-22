using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Mdsweep.Api.IntegrationTests;

public sealed class KeycloakRealmImportTests
{
    [Fact]
    public async Task Development_realm_imports_into_the_pinned_keycloak_version()
    {
        var importDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "keycloak");
        await using var keycloak = new ContainerBuilder()
            .WithImage("quay.io/keycloak/keycloak:26.2.5")
            .WithBindMount(importDirectory, "/opt/keycloak/data/import", AccessMode.ReadOnly)
            .WithCommand("start-dev", "--import-realm")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(
                "KC-SERVICES0032: Import finished successfully",
                strategy => strategy.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();

        await keycloak.StartAsync();
    }
}
