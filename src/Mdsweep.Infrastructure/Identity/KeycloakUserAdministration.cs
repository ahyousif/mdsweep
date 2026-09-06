using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mdsweep.Application.Users;
using Microsoft.Extensions.Options;

namespace Mdsweep.Infrastructure.Identity;

public sealed class KeycloakUserAdministration(HttpClient client,
    IOptions<KeycloakAdministrationOptions> options, IOptions<KeycloakAuthenticationOptions> authentication)
    : IIdentityAdministration
{
    private const string EmailFailure = "The email could not be sent. Ask an Administrator to check Keycloak email delivery, then retry.";
    private const string IdentityFailure = "The identity service is unavailable. Try again; if this continues, contact an Administrator.";
    private readonly string authority = authentication.Value.Authority.TrimEnd('/');

    public async Task InviteAsync(string organizationId, string email, string firstName, string lastName, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Post, $"organizations/{Escape(organizationId)}/members/invite-user",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = email,
                ["firstName"] = firstName,
                ["lastName"] = lastName,
            }), EmailFailure, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Signup may have completed before the local invitation expired. Keycloak's
            // invite-user rejects members, but invite-existing-user can send a fresh link.
            using var lookup = await Send(HttpMethod.Get, $"users?email={Escape(email)}&exact=true", null, EmailFailure, ct);
            if (!lookup.IsSuccessStatusCode) throw new IdentityAdministrationException(EmailFailure);
            var users = await lookup.Content.ReadFromJsonAsync<ExistingIdentity[]>(ct);
            var user = users?.SingleOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
            if (user is null) throw new IdentityAdministrationException(EmailFailure);
            using var resent = await Send(HttpMethod.Post, $"organizations/{Escape(organizationId)}/members/invite-existing-user",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = user.Id }), EmailFailure, ct);
            if (!resent.IsSuccessStatusCode) throw new IdentityAdministrationException(EmailFailure);
            return;
        }
        if (!response.IsSuccessStatusCode) throw new IdentityAdministrationException(EmailFailure);
    }

    public async Task<VerifiedIdentity?> GetVerifiedIdentityAsync(string subject, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Get, $"users/{Escape(subject)}", null, IdentityFailure, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new IdentityAdministrationException(IdentityFailure);
        var user = await response.Content.ReadFromJsonAsync<IdentityUser>(ct);
        return user is { Enabled: true, EmailVerified: true } && !string.IsNullOrWhiteSpace(user.Email)
            ? new VerifiedIdentity(subject, user.Email) : null;
    }

    public async Task<bool> IsOrganizationMemberAsync(string subject, string organizationId, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Get,
            $"organizations/{Escape(organizationId)}/members/{Escape(subject)}", null, IdentityFailure, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode) throw new IdentityAdministrationException(IdentityFailure);
        return true;
    }

    public async Task SendPasswordResetAsync(string subject, CancellationToken ct)
    {
        using var response = await Send(HttpMethod.Put, $"users/{Escape(subject)}/execute-actions-email",
            JsonContent.Create(new[] { "UPDATE_PASSWORD" }), EmailFailure, ct);
        if (!response.IsSuccessStatusCode) throw new IdentityAdministrationException(EmailFailure);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, HttpContent? content,
        string error, CancellationToken ct)
    {
        using (content)
        {
            try
            {
                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{authority}/protocol/openid-connect/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = options.Value.ClientId,
                        ["client_secret"] = options.Value.ClientSecret,
                    }),
                };
                using var tokenResponse = await client.SendAsync(tokenRequest, ct);
                if (!tokenResponse.IsSuccessStatusCode) throw new IdentityAdministrationException(error);
                using var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStreamAsync(ct));
                using var request = new HttpRequestMessage(method,
                    $"{authority.Replace("/realms/", "/admin/realms/", StringComparison.Ordinal)}/{path}")
                { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.RootElement.GetProperty("access_token").GetString());
                return await client.SendAsync(request, ct);
            }
            catch (HttpRequestException) { throw new IdentityAdministrationException(error); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new IdentityAdministrationException(error); }
        }
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private sealed record IdentityUser(string? Email, bool EmailVerified, bool Enabled);
    private sealed record ExistingIdentity(string Id, string Email);
}
