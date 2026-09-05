using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TenantContextTests : MdsweepIntegrationTest
{
    [Theory]
    [InlineData("/trips?date=2026-09-04", "/trips?date=2026-09-04")]
    [InlineData("https://attacker.example/", "/")]
    [InlineData("//attacker.example/", "/")]
    [InlineData("/\\attacker.example/", "/")]
    public async Task Login_only_preserves_safe_local_return_urls(string returnUrl, string expectedRedirectUri)
    {
        using var client = Application.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
        using var response = await client.GetAsync($"/api/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var state = QueryHelpers.ParseQuery(response.Headers.Location!.Query)["state"].Single();
        var oidc = Application.Services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);
        var properties = oidc.StateDataFormat.Unprotect(state);

        Assert.Equal(expectedRedirectUri, properties?.RedirectUri);
    }

    [Fact]
    public async Task AuthenticatedUserCanBootstrapAnActiveTenantSession()
    {
        using var client = Application.CreateClient();

        using var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Synthetic Dispatcher", document.RootElement.GetProperty("displayName").GetString());
        var activeTenant = document.RootElement.GetProperty("activeTenant");
        Assert.Equal("mdsw-eep2-3456", activeTenant.GetProperty("id").GetString());
        Assert.Equal("Synthetic Tenant", activeTenant.GetProperty("name").GetString());
        Assert.Contains(
            activeTenant.GetProperty("roles").EnumerateArray(),
            role => role.GetString() == "Dispatcher"
        );
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task UserCanSelectAnAuthorizedTenant()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/auth/tenant-context",
            new { tenantId = "mdsw-eep2-3456" }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotSelectATenantWithoutMembership()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/auth/tenant-context",
            new { tenantId = "abcd-efgh-jkmn" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
