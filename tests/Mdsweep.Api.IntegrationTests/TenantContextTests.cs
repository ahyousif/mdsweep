using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TenantContextTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task AuthenticatedUserCanListTenantMemberships()
    {
        using var client = Application.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var membership = document.RootElement[0];
        Assert.Equal("Synthetic", membership.GetProperty("firstName").GetString());
        Assert.Equal("Dispatcher", membership.GetProperty("lastName").GetString());
        Assert.Equal("mdsw-eep2-3456", membership.GetProperty("tenantId").GetString());
        Assert.Equal("Dispatcher", membership.GetProperty("role").GetString());
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
