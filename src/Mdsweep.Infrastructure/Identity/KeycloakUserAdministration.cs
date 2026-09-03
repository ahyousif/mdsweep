using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mdsweep.Infrastructure.Identity;

public interface IKeycloakUserAdministration
{
    Task<string> CreateDriverAsync(
        string email,
        string temporaryPassword,
        string organizationId,
        CancellationToken cancellationToken
    );
    Task ResetPasswordAsync(
        string subject,
        string temporaryPassword,
        CancellationToken cancellationToken
    );
    Task DeleteUserAsync(string subject, CancellationToken cancellationToken);
}

internal sealed class KeycloakUserAdministration(
    HttpClient client,
    IOptions<KeycloakAdministrationOptions> options,
    IOptions<KeycloakAuthenticationOptions> authentication)
    : IKeycloakUserAdministration
{
    private readonly string authority = authentication.Value.Authority;
    private readonly string clientId = options.Value.ClientId;
    private readonly string clientSecret = options.Value.ClientSecret;

    public async Task<string> CreateDriverAsync(
        string email,
        string temporaryPassword,
        string organizationId,
        CancellationToken cancellationToken
    )
    {
        var token = await AccessToken(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{AdminBase()}/users")
        {
            Content = JsonContent.Create(
                new
                {
                    username = email,
                    email,
                    enabled = true,
                    emailVerified = true,
                }
            ),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var subject = response.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
        if (string.IsNullOrWhiteSpace(subject))
            throw new InvalidOperationException(
                "Keycloak did not return the created user identifier."
            );
        try
        {
            await ResetPasswordAsync(subject, temporaryPassword, cancellationToken);
            await AddToOrganizationAsync(subject, organizationId, cancellationToken);
            return subject;
        }
        catch
        {
            await DeleteUserAsync(subject, CancellationToken.None);
            throw;
        }
    }

    private async Task AddToOrganizationAsync(
        string subject,
        string organizationId,
        CancellationToken cancellationToken
    )
    {
        var token = await AccessToken(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{AdminBase()}/organizations/{organizationId}/members"
        )
        {
            Content = JsonContent.Create(subject),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(
        string subject,
        string temporaryPassword,
        CancellationToken cancellationToken
    )
    {
        var token = await AccessToken(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{AdminBase()}/users/{subject}/reset-password"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    type = "password",
                    value = temporaryPassword,
                    temporary = true,
                }
            ),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string subject, CancellationToken cancellationToken)
    {
        var token = await AccessToken(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{AdminBase()}/users/{subject}"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> AccessToken(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{authority}/protocol/openid-connect/token"
        )
        {
            Content = new FormUrlEncodedContent([
                new("grant_type", "client_credentials"),
                new("client_id", clientId),
                new("client_secret", clientSecret),
            ]),
        };
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        return body.RootElement.GetProperty("access_token").GetString()!;
    }

    private string AdminBase() =>
        authority.Replace("/realms/", "/admin/realms/", StringComparison.Ordinal);
}
