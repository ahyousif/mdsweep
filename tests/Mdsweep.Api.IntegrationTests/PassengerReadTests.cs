using System.Net;
using System.Net.Http.Json;
using Mdsweep.Domain.Passengers;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class PassengerReadTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Dispatcher_lists_paginated_passengers_in_the_active_tenant()
    {
        await AddPassengerAsync("mdsw-eep2-3456", "Ada", "First", "MEMBER-ADA");
        await AddPassengerAsync("mdsw-eep2-3456", "Bea", "Second", "MEMBER-BEA");
        await AddPassengerAsync("mdsw-eep2-3456", "Cara", "Third", "MEMBER-CARA");
        await AddPassengerAsync("mdsw-other-000", "Other", "Tenant", "MEMBER-OTHER");
        using var client = Application.CreateClient();

        var firstPage = await GetPassengers(client, "/api/passengers?page=1&pageSize=2");
        var secondPage = await GetPassengers(client, "/api/passengers?page=2&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(["Ada", "Bea"], firstPage.Items.Select(passenger => passenger.FirstName));
        Assert.Single(secondPage.Items);
        Assert.Equal("Cara", secondPage.Items[0].FirstName);
        Assert.DoesNotContain(firstPage.Items, passenger => passenger.FirstName == "Other");
    }

    [Theory]
    [InlineData("ada")]
    [InlineData("lovelace")]
    [InlineData("member-42")]
    [InlineData("555-0100")]
    [InlineData("555-0199")]
    public async Task Dispatcher_searches_passenger_profile_fields_case_insensitively(string search)
    {
        await AddPassengerAsync(
            "mdsw-eep2-3456",
            "Ada",
            "Lovelace",
            "MEMBER-42",
            phoneNumber: "555-0100",
            alternatePhoneNumber: "555-0199"
        );
        await AddPassengerAsync("mdsw-eep2-3456", "Other", "Passenger", "MEMBER-OTHER");
        using var client = Application.CreateClient();

        var result = await GetPassengers(client, $"/api/passengers?search={Uri.EscapeDataString(search)}");

        Assert.Single(result.Items);
        Assert.Equal("Ada", result.Items[0].FirstName);
    }

    [Fact]
    public async Task Dispatcher_gets_a_passenger_profile_without_trip_history()
    {
        var passengerId = await AddPassengerAsync(
            "mdsw-eep2-3456",
            "Ada",
            "Lovelace",
            "MEMBER-42",
            new LocalDate(1980, 1, 2),
            "555-0100",
            "555-0199",
            "Wheel Chair",
            "Cannot Transfer",
            "Use the east entrance."
        );
        using var client = Application.CreateClient();

        using var response = await client.GetAsync($"/api/passengers/{passengerId}");
        response.EnsureSuccessStatusCode();
        var passenger = await response.Content.ReadFromJsonAsync<PassengerDetailResponse>();

        Assert.NotNull(passenger);
        Assert.Equal(passengerId, passenger.Id);
        Assert.Equal("MEMBER-42", passenger.BrokerMemberId);
        Assert.Equal(new DateOnly(1980, 1, 2), passenger.DateOfBirth);
        Assert.Equal("555-0100", passenger.PhoneNumber);
        Assert.Equal("555-0199", passenger.AlternatePhoneNumber);
        Assert.Equal("Wheel Chair", passenger.PassengerType);
        Assert.Equal("Cannot Transfer", passenger.SpecialNeeds);
        Assert.Equal("Use the east entrance.", passenger.Notes);
    }

    [Fact]
    public async Task Unknown_or_other_tenant_passenger_returns_not_found()
    {
        var otherTenantPassengerId = await AddPassengerAsync(
            "mdsw-other-000",
            "Other",
            "Tenant",
            "MEMBER-OTHER"
        );
        using var client = Application.CreateClient();

        using var unknown = await client.GetAsync($"/api/passengers/{Guid.CreateVersion7()}");
        using var otherTenant = await client.GetAsync($"/api/passengers/{otherTenantPassengerId}");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherTenant.StatusCode);
    }

    private static async Task<PagedPassengerResponse> GetPassengers(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedPassengerResponse>())!;
    }

    private async Task<Guid> AddPassengerAsync(
        string tenantId,
        string firstName,
        string lastName,
        string brokerMemberId,
        LocalDate? dateOfBirth = null,
        string? phoneNumber = null,
        string? alternatePhoneNumber = null,
        string? passengerType = null,
        string? specialNeeds = null,
        string? notes = null
    )
    {
        await using var db = CreateDbContext();
        var passenger = PassengerAggregate.Create(
            brokerMemberId,
            firstName,
            lastName,
            dateOfBirth,
            phoneNumber,
            alternatePhoneNumber,
            passengerType,
            specialNeeds
        );
        passenger.TenantId = tenantId;
        passenger.UpdateNotes(notes);
        db.Add(passenger);
        await db.SaveChangesAsync();
        return passenger.Id;
    }

    private ApplicationDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
                .UseSnakeCaseNamingConvention()
                .Options
        );

    private sealed record PagedPassengerResponse(
        List<PassengerListResponse> Items,
        long TotalCount,
        int Page,
        int PageSize,
        long TotalPages
    );

    private sealed record PassengerListResponse(
        Guid Id,
        string? BrokerMemberId,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? PassengerType
    );

    private sealed record PassengerDetailResponse(
        Guid Id,
        string? BrokerMemberId,
        string FirstName,
        string LastName,
        DateOnly? DateOfBirth,
        string? PhoneNumber,
        string? AlternatePhoneNumber,
        string? PassengerType,
        string? SpecialNeeds,
        string? Notes
    );
}
