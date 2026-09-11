using System.Text.Json;

namespace Mdsweep.Api.IntegrationTests;

public sealed class KeycloakRealmImportTests
{
    [Fact]
    public void Development_realm_registers_the_oidc_sign_in_and_sign_out_callbacks()
    {
        var realmPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "keycloak",
            "mdsweep-realm.json"
        );
        using var realm = JsonDocument.Parse(File.ReadAllText(realmPath));
        Assert.True(realm.RootElement.GetProperty("registrationAllowed").GetBoolean());
        var redirectUris = realm.RootElement
            .GetProperty("clients")
            .EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == "mdsweep-server")
            .GetProperty("redirectUris")
            .EnumerateArray()
            .Select(uri => uri.GetString())
            .ToList();

        Assert.Contains("http://localhost:4200/signin-oidc", redirectUris);
        Assert.Contains("http://localhost:4200/signout-callback-oidc", redirectUris);

        var postLogoutRedirectUris = realm.RootElement
            .GetProperty("clients")
            .EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == "mdsweep-server")
            .GetProperty("attributes")
            .GetProperty("post.logout.redirect.uris")
            .GetString();

        Assert.Equal("http://localhost:4200/signout-callback-oidc", postLogoutRedirectUris);

        Assert.Equal("mdsweep-browser", realm.RootElement.GetProperty("browserFlow").GetString());
        var browserFlow = realm.RootElement
            .GetProperty("authenticationFlows")
            .EnumerateArray()
            .Single(flow => flow.GetProperty("alias").GetString() == "mdsweep-browser");
        Assert.Contains(
            browserFlow.GetProperty("authenticationExecutions").EnumerateArray(),
            execution =>
                execution.GetProperty("authenticatorFlow").GetBoolean()
                && execution.GetProperty("flowAlias").GetString() == "mdsweep-browser-forms"
        );
        Assert.False(
            realm.RootElement
                .GetProperty("clients")
                .EnumerateArray()
                .Single(client => client.GetProperty("clientId").GetString() == "mdsweep-server")
                .TryGetProperty("protocolMappers", out _)
        );
    }
}
