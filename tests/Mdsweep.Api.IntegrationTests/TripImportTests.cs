using System.Net;
using System.Net.Http.Json;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TripImportTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Dispatcher_can_preview_then_apply_a_csv_trip_import_once()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var previewResponse = await Upload(client, Csv());
        Assert.Equal(HttpStatusCode.Created, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);
        Assert.Equal("Previewed", preview.Status);
        Assert.Single(preview.Rows);
        Assert.Equal("Ready", preview.Rows[0].Disposition);

        using var applyResponse = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(applied);
        Assert.Equal("Applied", applied.Status);

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.Passengers.IgnoreQueryFilters().ToListAsync());
        Assert.Single(await db.Trips.IgnoreQueryFilters().ToListAsync());

        using var duplicateApply = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        Assert.Equal(HttpStatusCode.Conflict, duplicateApply.StatusCode);
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, string csv)
    {
        var form = new MultipartFormDataContent { { new StringContent(csv), "file", "trips.csv" } };
        return client.PostAsync("/api/trip-imports", form);
    }

    private static string Csv() =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag\n"
        + "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N";

    private sealed record TripImportResponse(Guid Id, string Status, List<TripImportRowResponse> Rows);
    private sealed record TripImportRowResponse(string Disposition);
}
