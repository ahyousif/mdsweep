using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mdsweep.Api.Infrastructure;
using Mdsweep.Api.Features.ManifestImports;
using Testcontainers.PostgreSql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ManifestPreviewTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();
    private WebApplicationFactory<Program> application = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(database.GetConnectionString()));
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
            });
        });
    }

    [Fact]
    public async Task Preview_reports_exceptions_without_persisting_trips()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await using var file = File.OpenRead(FixturePath("mtm-manifest.csv"));
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", "mtm-manifest.csv" }
        };

        using var response = await client.PostAsync("/api/manifest-imports/preview", form);

        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(preview);
        Assert.Equal(2, preview.Ready);
        Assert.Equal(1, preview.Warning);
        Assert.Equal(1, preview.Blocked);
        Assert.Equal(4, preview.Rows.Count);
        Assert.Equal([new DateOnly(2026, 9, 15)], preview.ServiceDates);

        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.Trips.CountAsync());
    }

    [Fact]
    public async Task Applying_preview_imports_trips_groups_journeys_and_retains_blocked_rows()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await Preview(client, "mtm-manifest.csv");

        using var applyResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);

        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.NotNull(applied);
        Assert.Equal(3, applied.Imported);
        Assert.Equal(1, applied.Blocked);

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        Assert.NotNull(serviceDay);
        Assert.Equal(3, serviceDay.Count);
        Assert.Equal(2, serviceDay.Count(x => x.JourneyKey == "SYNTH100"));
        Assert.Contains(serviceDay, x => x.TripNumber == "SYNTH200A" && !x.IsActive);

        var retainedPreview = await client.GetFromJsonAsync<PreviewResponse>($"/api/manifest-imports/{preview.PreviewId}");
        Assert.NotNull(retainedPreview);
        Assert.Contains(retainedPreview.Rows, x => x.TripNumber == "SYNTH300A" && x.Disposition == "Blocked");

        using var retryResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        retryResponse.EnsureSuccessStatusCode();
        var afterRetry = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        Assert.Equal(3, afterRetry!.Count);
    }

    [Fact]
    public async Task Preview_blocks_every_row_with_a_duplicate_trip_number()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(client, Manifest(
            Row("DUPLICATE1", "VALID", "0915", "100 First St", "200 Main St"),
            Row("DUPLICATE1", "VALID", "1015", "300 Second St", "400 Oak St")));

        Assert.Equal(0, preview.Ready);
        Assert.Equal(0, preview.Warning);
        Assert.Equal(2, preview.Blocked);
        Assert.All(preview.Rows, row =>
            Assert.Contains(row.Messages, message => message.Contains("more than once", StringComparison.OrdinalIgnoreCase)));

        using var applyResponse = await client.PostAsync($"/api/manifest-imports/{preview.PreviewId}/apply", null);
        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.Equal(0, applied!.Imported);
    }

    [Fact]
    public async Task Revised_manifest_reconciles_broker_fields_and_keeps_import_history()
    {
        using var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var original = await PreviewCsv(client, Manifest(Row("REVISED1", "VALID", "0915", "100 First St", "200 Main St")));
        using var firstApply = await client.PostAsync($"/api/manifest-imports/{original.PreviewId}/apply", null);
        firstApply.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(client, Manifest(Row("REVISED1", "TURN BACK", "1030", "300 New St", "400 Changed St")));
        using var secondApply = await client.PostAsync($"/api/manifest-imports/{revised.PreviewId}/apply", null);
        secondApply.EnsureSuccessStatusCode();

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>("/api/service-days/2026-09-15/trips");
        var trip = Assert.Single(serviceDay!);
        Assert.Equal("TURN BACK", trip.BrokerStatus);
        Assert.Equal(new TimeOnly(10, 30), trip.AppointmentTime);
        Assert.Equal("300 New St", trip.PickupAddress);
        Assert.False(trip.IsActive);

        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var history = await db.TripBrokerImports.OrderBy(x => x.ImportedAt).ToListAsync();
        Assert.Equal(["VALID", "TURN BACK"], history.Select(x => x.BrokerStatus));
        Assert.Equal(["100 First St", "300 New St"], history.Select(x => x.PickupAddress));
    }

    public async Task DisposeAsync()
    {
        await application.DisposeAsync();
        await database.DisposeAsync();
    }

    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private static async Task<PreviewResponse> Preview(HttpClient client, string fixture)
    {
        await using var file = File.OpenRead(FixturePath(fixture));
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", fixture }
        };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    private static async Task<PreviewResponse> PreviewCsv(HttpClient client, string csv, string fileName = "manifest.csv")
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(csv), "file", fileName }
        };
        using var response = await client.PostAsync("/api/manifest-imports/preview", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreviewResponse>())!;
    }

    private static string Manifest(params string[] rows) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Passenger Type,Vehicle Type,Will Call Flag\n" +
        string.Join('\n', rows);

    private static string Row(string tripNumber, string status, string time, string pickup, string delivery) =>
        $"09/15/2026,{delivery},{pickup},{time},{tripNumber},{status},Test,Rider,Phoenix,Mesa,Ambulatory,Cab,N";

    private sealed record PreviewResponse(Guid PreviewId, int Ready, int Warning, int Blocked, List<DateOnly> ServiceDates, List<PreviewRow> Rows);
    private sealed record PreviewRow(string TripNumber, string Disposition, IReadOnlyList<string> Messages);
    private sealed record ApplyResponse(int Imported, int Blocked);
    private sealed record ServiceDayTrip(
        string TripNumber,
        string JourneyKey,
        string BrokerStatus,
        TimeOnly AppointmentTime,
        string PickupAddress,
        bool IsActive);
}
