using System.Net;
using System.Net.Http.Json;

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
            new
            {
                brokerMemberId = "MEM-SYNTH-1001",
                firstName = "Jordan",
                lastName = "Example",
                phoneNumber = "555-0101",
                notes = "Synthetic mobility note",
            }
        );

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PassengerResponse>();
        Assert.NotNull(created);
        Assert.Equal(7, created.Id.Version);
        Assert.Equal("MEM-SYNTH-1001", created.BrokerMemberId);
        Assert.Equal("Jordan", created.FirstName);
        Assert.Equal("Example", created.LastName);
        Assert.Equal("555-0101", created.PhoneNumber);
        Assert.Equal("Synthetic mobility note", created.Notes);
        Assert.NotNull(createResponse.Headers.Location);

        using var getResponse = await client.GetAsync(createResponse.Headers.Location);
        getResponse.EnsureSuccessStatusCode();
        var retrieved = await getResponse.Content.ReadFromJsonAsync<PassengerResponse>();
        Assert.Equal(created, retrieved);
    }

    private sealed record PassengerResponse(
        Guid Id,
        string? BrokerMemberId,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? Notes
    );
}
