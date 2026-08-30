using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mdsweep.Application.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ProviderContextTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Authenticated_user_lists_local_provider_memberships()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var contexts = await client.GetFromJsonAsync<List<ProviderContext>>("/api/auth/me");

        var context = Assert.Single(contexts!);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), context.ProviderId);
        Assert.Equal("Dispatcher", context.Role);
    }

    [Fact]
    public async Task Authenticated_user_can_select_a_local_provider_context()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var providerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        using var response = await client.PostAsJsonAsync(
            "/api/auth/provider-context",
            new { providerId }
        );

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(".Mdsweep.Auth=", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task Active_provider_without_a_local_membership_is_forbidden()
    {
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ProviderMemberships.Remove(await db.ProviderMemberships.SingleAsync());
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var response = await client.GetAsync("/api/drivers");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }
}
