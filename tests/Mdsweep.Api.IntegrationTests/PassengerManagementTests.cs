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
    public async Task Cookie_authenticated_json_mutation_rejects_an_invalid_antiforgery_token()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid-token");

        using var response = await client.PostAsJsonAsync(
            "/api/passengers",
            new { firstName = "Jordan", lastName = "Example" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_authenticated_get_requests_do_not_require_an_antiforgery_token()
    {
        using var client = Application.CreateClient();

        using var response = await client.GetAsync("/api/trips");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    [Fact]
    public async Task Updating_details_of_a_disabled_passenger_preserves_inactive_status()
    {
        var id = await AddPassengerAsync();
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var disable = await client.PostAsync($"/api/passengers/{id}/disable", null);
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        using var update = await client.PutAsJsonAsync(
            $"/api/passengers/{id}",
            new { firstName = "Updated", lastName = "Example", brokerMemberId = "MEMBER-42" }
        );
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        using var staleUpdate = await client.PutAsJsonAsync(
            $"/api/passengers/{id}",
            new { firstName = "Stale", lastName = "Example", brokerMemberId = "MEMBER-42", isActive = true }
        );
        Assert.Equal(HttpStatusCode.BadRequest, staleUpdate.StatusCode);

        using var response = await client.GetAsync($"/api/passengers/{id}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Updated", document.RootElement.GetProperty("firstName").GetString());
        Assert.False(document.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Lifecycle_endpoints_are_idempotent_and_preserve_passenger_details()
    {
        var id = await AddPassengerAsync();
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        foreach (var expectedActive in new[] { false, false, true, true })
        {
            var action = expectedActive ? "enable" : "disable";
            using var response = await client.PostAsync($"/api/passengers/{id}/{action}", null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(expectedActive, await IsActiveAsync(id));
        }

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passenger = await db.Passengers.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
        Assert.Equal("Jordan", passenger.FirstName);
        Assert.Equal("Example", passenger.LastName);
    }

    [Fact]
    public async Task Missing_passenger_returns_not_found_for_details_and_lifecycle()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var id = Guid.CreateVersion7();

        using var update = await client.PutAsJsonAsync(
            $"/api/passengers/{id}", new { firstName = "Jordan", lastName = "Example" }
        );
        using var disable = await client.PostAsync($"/api/passengers/{id}/disable", null);
        using var enable = await client.PostAsync($"/api/passengers/{id}/enable", null);

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, disable.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enable.StatusCode);
    }

    [Fact]
    public async Task User_without_dispatcher_membership_cannot_change_passenger_lifecycle()
    {
        var id = await AddPassengerAsync();
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TenantMemberships.Remove(await db.TenantMemberships.SingleAsync());
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var disable = await client.PostAsync($"/api/passengers/{id}/disable", null);
        using var enable = await client.PostAsync($"/api/passengers/{id}/enable", null);

        Assert.Equal(HttpStatusCode.Forbidden, disable.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, enable.StatusCode);
        Assert.True(await IsActiveAsync(id));
    }

    private async Task<Guid> AddPassengerAsync()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var response = await client.PostAsJsonAsync(
            "/api/passengers", new { firstName = "Jordan", lastName = "Example", brokerMemberId = "MEMBER-42" }
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedPassengerResponse>())!.Id;
    }

    private async Task<bool> IsActiveAsync(Guid id)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.Passengers.IgnoreQueryFilters().SingleAsync(x => x.Id == id)).IsActive;
    }

    private sealed record CreatedPassengerResponse(Guid Id, string FirstName, string LastName);
}
