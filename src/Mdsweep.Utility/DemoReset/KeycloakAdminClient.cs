using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mdsweep.Utility.DemoReset;

public sealed class KeycloakAdminClient(HttpClient httpClient, IOptions<KeycloakAdministrationOptions> options)
{
    private const string RealmName = "mdsweep";
    private readonly KeycloakAdministrationOptions configuration = options.Value;

    public async Task<string> RecreateDemoRealmAsync(string webBaseUrl, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        await DeleteRealmIfPresentAsync(accessToken, cancellationToken);
        await CreateRealmAsync(accessToken, cancellationToken);
        await CreateServerClientAsync(accessToken, webBaseUrl, cancellationToken);
        return await CreateDemoAdministratorAsync(accessToken, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "realms/master/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = configuration.AutomationClientId,
                    ["client_secret"] = configuration.AutomationClientSecret,
                }
            ),
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var token = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return token.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Keycloak did not return an access token.");
    }

    private async Task DeleteRealmIfPresentAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = Authorized(HttpMethod.Delete, $"admin/realms/{RealmName}", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task CreateRealmAsync(string accessToken, CancellationToken cancellationToken)
    {
        var realm = new { realm = RealmName, enabled = true, registrationAllowed = true };
        await SendJsonAsync(HttpMethod.Post, "admin/realms", accessToken, realm, cancellationToken);
    }

    private async Task CreateServerClientAsync(string accessToken, string webBaseUrl, CancellationToken cancellationToken)
    {
        var baseUrl = webBaseUrl.TrimEnd('/');
        var client = new
        {
            clientId = "mdsweep-api",
            enabled = true,
            publicClient = false,
            secret = configuration.OidcClientSecret,
            protocol = "openid-connect",
            standardFlowEnabled = true,
            redirectUris = new[] { $"{baseUrl}/signin-oidc", $"{baseUrl}/signout-callback-oidc" },
            webOrigins = new[] { baseUrl },
            attributes = new Dictionary<string, string>
            {
                ["post.logout.redirect.uris"] = $"{baseUrl}/signout-callback-oidc",
            },
        };
        await SendJsonAsync(HttpMethod.Post, $"admin/realms/{RealmName}/clients", accessToken, client, cancellationToken);
    }

    private async Task<string> CreateDemoAdministratorAsync(string accessToken, CancellationToken cancellationToken)
    {
        var user = new
        {
            username = "demo.admin@mdsweep.test",
            email = "demo.admin@mdsweep.test",
            firstName = "Demo",
            lastName = "Administrator",
            enabled = true,
            emailVerified = true,
            credentials = new[] { new { type = "password", value = configuration.DemoAdminPassword, temporary = false } },
        };

        using var request = Authorized(HttpMethod.Post, $"admin/realms/{RealmName}/users", accessToken);
        request.Content = JsonContent(user);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("Keycloak did not return the demo administrator ID.");
        }

        return location.TrimEnd('/').Split('/').Last();
    }

    private async Task SendJsonAsync<T>(
        HttpMethod method,
        string path,
        string accessToken,
        T body,
        CancellationToken cancellationToken
    )
    {
        using var request = Authorized(method, path, accessToken);
        request.Content = JsonContent(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
