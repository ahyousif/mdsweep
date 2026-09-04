using System.Net;
using System.Net.Http.Json;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TripImportTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Csv_import_adds_trips_and_reimport_is_unchanged()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var first = await Upload(client, Csv());
        var firstResult = await first.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(firstResult); Assert.Equal(1, firstResult.Added);
        using var repeat = await Upload(client, Csv());
        var repeatResult = await repeat.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(repeatResult); Assert.Equal(1, repeatResult.Unchanged);
        await using var scope = Application.Services.CreateAsyncScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Trips.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Xlsx_import_adds_trips()
    {
        using var client = Application.CreateClient(); await AddAntiforgeryToken(client);
        using var response = await Upload(client, "trips.xlsx", Xlsx());
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result); Assert.Equal(1, result.Added);
    }

    [Fact]
    public async Task Import_normalizes_passenger_mobility_and_derives_wheelchair_capability()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await Upload(
            client,
            "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag,Passenger Type,Special Needs,Vehicle Type\n" +
            "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-MOBILITY,MED-MOBILITY,VALID,Synthetic,Passenger,Phoenix,Mesa,N,Wheel Chair,Cannot Transfer,Paralift"
        );
        response.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var trip = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Trips.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PassengerMobilityRequirement.ManualWheelchairCannotTransfer, trip.BrokerData.MobilityRequirement);
        Assert.Equal(RequiredVehicleCapability.WheelchairAccessible, trip.BrokerData.RequiredVehicleCapability);
        Assert.Equal("Wheel Chair", trip.BrokerData.RawImportedPassengerType);
    }

    [Fact]
    public async Task Changed_broker_data_preserves_scheduled_pickup_time()
    {
        using var client = Application.CreateClient(); await AddAntiforgeryToken(client);
        using var initial = await Upload(client, Csv()); initial.EnsureSuccessStatusCode();
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Trips.IgnoreQueryFilters().SingleAsync()).SetScheduledPickupTime(new LocalTime(8, 30));
            await db.SaveChangesAsync();
        }
        using var changed = await Upload(client, Csv("09/16/2026,201 Way,101 St,10:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N"));
        var result = await changed.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result); Assert.Equal(1, result.Updated);
        await using var verification = Application.Services.CreateAsyncScope();
        var trip = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Trips.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(new LocalTime(8, 30), trip.ScheduledPickupTime);
    }

    [Fact]
    public async Task Duplicate_numbers_are_skipped_while_valid_rows_import()
    {
        using var client = Application.CreateClient(); await AddAntiforgeryToken(client);
        using var response = await Upload(client, Csv(
            "09/15/2026,200 Way,100 St,09:15,TRIP-DUP,MED-1,VALID,First,Passenger,Phoenix,Mesa,N\n" +
            "09/15/2026,200 Way,100 St,09:15,TRIP-DUP,MED-2,VALID,Second,Passenger,Phoenix,Mesa,N\n" +
            "09/15/2026,200 Way,100 St,09:15,TRIP-GOOD,MED-3,VALID,Good,Passenger,Phoenix,Mesa,N"));
        var result = await response.Content.ReadFromJsonAsync<ImportTripsResponse>();
        Assert.NotNull(result); Assert.Equal(1, result.Added); Assert.Equal(2, result.NeedsAttention);
    }

    [Fact]
    public async Task Missing_required_column_rejects_the_file_without_mutation()
    {
        using var client = Application.CreateClient(); await AddAntiforgeryToken(client);
        using var response = await Upload(client, "trips.csv", System.Text.Encoding.UTF8.GetBytes("Trip Number\nTRIP-1"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, string csv) => Upload(client, "trips.csv", System.Text.Encoding.UTF8.GetBytes(csv));
    private static Task<HttpResponseMessage> Upload(HttpClient client, string name, byte[] content) => client.PostAsync("/api/trips/import", new MultipartFormDataContent { { new ByteArrayContent(content), "file", name } });
    private static string Csv(string row = "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N") => "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag\n" + row;
    private static byte[] Xlsx()
    {
        using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet("Trips");
        var headers = Csv().Split('\n')[0].Split(','); for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var row = Csv().Split('\n')[1].Split(','); for (var i = 0; i < row.Length; i++) sheet.Cell(2, i + 1).Value = row[i];
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }
    private sealed record ImportTripsResponse(int Added, int Updated, int Unchanged, int NeedsAttention, List<TripImportProblem> Problems);
    private sealed record TripImportProblem(int RowNumber, string? TripNumber, string Message);
}
