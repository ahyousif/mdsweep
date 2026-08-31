using System.Net;
using System.Net.Http.Json;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class PassengerManagementTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task DispatcherCanCreatePassengerIndependentlyOfManifest()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var createResponse = await client.PostAsJsonAsync(
            "/api/passengers",
            new { firstName = "Jordan", lastName = "Example" }
        );

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedPassengerResponse>();
        Assert.NotNull(created);
        Assert.Equal(7, created.Id.Version);
        Assert.Equal("Jordan", created.FirstName);
        Assert.Equal("Example", created.LastName);
        Assert.NotNull(createResponse.Headers.Location);
    }

    [Fact]
    public async Task UserWithoutDispatcherMembershipCannotCreatePassenger()
    {
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TenantMemberships.Remove(await db.TenantMemberships.SingleAsync());
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/passengers",
            new { firstName = "Jordan", lastName = "Example" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record CreatedPassengerResponse(Guid Id, string FirstName, string LastName);
}
