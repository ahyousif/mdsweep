using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

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
    }

    [Fact]
    public async Task Development_realm_supports_the_application_administration_workflow()
    {
        var importDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "keycloak");
        await using var keycloak = new ContainerBuilder()
            .WithImage("quay.io/keycloak/keycloak:26.2.5")
            .WithBindMount(importDirectory, "/opt/keycloak/data/import", AccessMode.ReadOnly)
            .WithPortBinding(8080, true)
            .WithCommand("start-dev", "--import-realm")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request =>
                        request
                            .ForPort(8080)
                            .ForPath("/realms/mdsweep/.well-known/openid-configuration")
                    )
            )
            .Build();

        await keycloak.StartAsync();

        using var client = new HttpClient
        {
            BaseAddress = new Uri(
                $"http://{keycloak.Hostname}:{keycloak.GetMappedPublicPort(8080)}"
            ),
        };
        using var tokenResponse = await client.PostAsync(
            "/realms/mdsweep/protocol/openid-connect/token",
            new FormUrlEncodedContent([
                new("grant_type", "client_credentials"),
                new("client_id", "mdsweep-administration"),
                new("client_secret", "Development-only-administration-secret"),
            ])
        );
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenBody = JsonDocument.Parse(await tokenResponse.Content.ReadAsStreamAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenBody.RootElement.GetProperty("access_token").GetString()
        );

        var email = $"driver-{Guid.CreateVersion7():N}@example.test";
        using var createResponse = await client.PostAsJsonAsync(
            "/admin/realms/mdsweep/users",
            new
            {
                username = email,
                email,
                enabled = true,
                emailVerified = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var subject = createResponse.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
        Assert.False(string.IsNullOrWhiteSpace(subject));

        using var resetResponse = await client.PutAsJsonAsync(
            $"/admin/realms/mdsweep/users/{subject}/reset-password",
            new
            {
                type = "password",
                value = "P@ssw0rd!",
                temporary = true,
            }
        );
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        using var membershipResponse = await client.PostAsJsonAsync(
            "/admin/realms/mdsweep/organizations/b6d8ea17-b31c-45c0-b5f2-4fec5df7c6cf/members",
            subject
        );
        Assert.Equal(HttpStatusCode.Created, membershipResponse.StatusCode);
    }
}
