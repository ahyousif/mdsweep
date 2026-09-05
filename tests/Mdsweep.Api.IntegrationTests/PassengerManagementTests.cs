using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class PassengerManagementTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Cookie_authenticated_json_mutation_requires_an_antiforgery_token()
    {
        using var client = Application.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/passengers",
            new { firstName = "Jordan", lastName = "Example" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passenger = await db.Passengers.IgnoreQueryFilters().SingleAsync(x => x.Id == created.Id);
        Assert.Equal("mdsw-eep2-3456", passenger.TenantId);
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

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    public async Task Dispatcher_receives_validation_details_for_blank_passenger_names(string blankField)
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var request = new Dictionary<string, string>
        {
            ["firstName"] = blankField == "firstName" ? "   " : "Jordan",
            ["lastName"] = blankField == "lastName" ? "   " : "Example",
        };

        using var response = await client.PostAsJsonAsync("/api/passengers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var errors = document.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty(blankField, out var messages));
        Assert.Contains(messages.EnumerateArray(), message => message.GetString()!.Contains("required"));
    }

    private sealed record CreatedPassengerResponse(Guid Id, string FirstName, string LastName);
}
