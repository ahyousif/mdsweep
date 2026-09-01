using System.Net;
using System.Net.Http.Json;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
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
        Assert.Single(preview.Items);
        Assert.Equal("Ready", preview.Items[0].Disposition);

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

    [Fact]
    public async Task Csv_quoted_commas_and_multiline_values_are_preserved_through_apply()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var previewResponse = await Upload(
            client,
            Csv(
                "09/15/2026,\"200 Synthetic Way, Suite 2\",\"100 Sample St\",09:15,TRIP-101,MED-101,VALID,Synthetic,Passenger,Phoenix,Mesa,N\n"
                    + "09/16/2026,200 Way,\"100 Sample\nStreet\",10:15,TRIP-102,MED-101,VALID,Synthetic,Passenger,Phoenix,Mesa,N"
            )
        );
        previewResponse.EnsureSuccessStatusCode();
        var preview = await previewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);
        Assert.Equal(2, preview.Items.Count);

        using var applyResponse = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        applyResponse.EnsureSuccessStatusCode();

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.Trips.IgnoreQueryFilters().CountAsync());
        Assert.Single(await db.Passengers.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Csv_invalid_and_malformed_values_return_validation_responses()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var invalidDate = await Upload(
            client,
            Csv("abc,200 Way,100 St,not-a-time,TRIP-103,MED-103,VALID,Test,Passenger,Phoenix,Mesa,N")
        );
        invalidDate.EnsureSuccessStatusCode();
        var preview = await invalidDate.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);
        Assert.Contains("Appointment Date 'abc' is invalid.", preview.Items[0].Messages);
        Assert.Contains("Appointment Time 'not-a-time' is invalid.", preview.Items[0].Messages);

        using var malformed = await Upload(
            client,
            Csv("09/15/2026,\"unterminated,100 St,09:15,TRIP-104,MED-104,VALID,Test,Passenger,Phoenix,Mesa,N")
        );
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    [Fact]
    public async Task Xlsx_blank_middle_cells_keep_their_columns_when_previewed_and_applied()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var previewResponse = await Upload(client, "trips.xlsx", Xlsx());
        previewResponse.EnsureSuccessStatusCode();
        var preview = await previewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);
        Assert.Equal("Ready", preview.Items[0].Disposition);

        using var applyResponse = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        applyResponse.EnsureSuccessStatusCode();
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var trip = await db.Trips.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("TRIP-105", trip.BrokerTripNumber);
        Assert.Equal("Mesa", trip.BrokerData.DropoffCity);
    }

    [Fact]
    public async Task Apply_conflict_after_preview_persists_no_partial_changes()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var previewResponse = await Upload(
            client,
            Csv(
                "09/15/2026,200 Way,100 St,09:15,TRIP-106,MED-106,VALID,First,Passenger,Phoenix,Mesa,N\n"
                    + "09/16/2026,201 Way,101 St,10:15,TRIP-107,MED-107,VALID,Second,Passenger,Phoenix,Mesa,N"
            )
        );
        previewResponse.EnsureSuccessStatusCode();
        var preview = await previewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);

        await using (var db = CreateSeedDb())
        {
            var otherPassenger = PassengerAggregate.Create("MED-OTHER", "Other", "Passenger");
            otherPassenger.TenantId = "mdsw-eep2-3456";
            var existingTrip = TripAggregate.Create(
                otherPassenger.Id,
                "TRIP-107",
                new BrokerTripData(
                    new DateOnly(2026, 9, 16),
                    null,
                    "201 Way",
                    "Phoenix",
                    "101 St",
                    "Mesa",
                    "VALID",
                    false
                )
            );
            existingTrip.TenantId = "mdsw-eep2-3456";
            db.AddRange(otherPassenger, existingTrip);
            await db.SaveChangesAsync();
        }

        using var applyResponse = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        Assert.Equal(HttpStatusCode.Conflict, applyResponse.StatusCode);

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await verificationDb.Passengers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await verificationDb.Trips.IgnoreQueryFilters().CountAsync());
        var import = await verificationDb.TripImports.IgnoreQueryFilters().Include(value => value.Items).SingleAsync();
        Assert.Equal("Previewed", import.Status.ToString());
        Assert.All(import.Items, item => Assert.Null(item.AppliedTripId));
    }

    [Fact]
    public async Task Existing_trip_with_a_different_passenger_is_blocked_in_preview_and_apply()
    {
        await using (var db = CreateSeedDb())
        {
            var passenger = PassengerAggregate.Create("MED-OLD", "Old", "Passenger");
            passenger.TenantId = "mdsw-eep2-3456";
            var trip = TripAggregate.Create(
                passenger.Id,
                "TRIP-108",
                new BrokerTripData(
                    new DateOnly(2026, 9, 15),
                    null,
                    "100 St",
                    "Phoenix",
                    "200 Way",
                    "Mesa",
                    "VALID",
                    false
                )
            );
            trip.TenantId = "mdsw-eep2-3456";
            db.AddRange(passenger, trip);
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var previewResponse = await Upload(
            client,
            Csv("09/15/2026,200 Way,100 St,09:15,TRIP-108,MED-NEW,VALID,New,Passenger,Phoenix,Mesa,N")
        );
        previewResponse.EnsureSuccessStatusCode();
        var preview = await previewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(preview);
        Assert.Equal("Blocked", preview.Items[0].Disposition);
        Assert.Contains("Trip TRIP-108 already belongs to a different passenger.", preview.Items[0].Messages);

        using var applyResponse = await client.PostAsync($"/api/trip-imports/{preview.Id}/apply", null);
        Assert.Equal(HttpStatusCode.Conflict, applyResponse.StatusCode);
    }

    [Fact]
    public async Task Duplicate_trip_numbers_are_blocked_and_only_one_matching_preview_can_be_applied()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var duplicate = Csv(
            "09/15/2026,200 Way,100 St,09:15,TRIP-109,MED-109,VALID,First,Passenger,Phoenix,Mesa,N\n"
                + "09/16/2026,201 Way,101 St,10:15,TRIP-109,MED-110,VALID,Second,Passenger,Phoenix,Mesa,N"
        );
        using var duplicatePreviewResponse = await Upload(client, duplicate);
        duplicatePreviewResponse.EnsureSuccessStatusCode();
        var duplicatePreview = await duplicatePreviewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(duplicatePreview);
        Assert.All(duplicatePreview.Items, item => Assert.Equal("Blocked", item.Disposition));

        using var firstPreviewResponse = await Upload(
            client,
            Csv("09/15/2026,200 Way,100 St,09:15,TRIP-110,MED-111,VALID,Test,Passenger,Phoenix,Mesa,N")
        );
        using var secondPreviewResponse = await Upload(
            client,
            Csv("09/15/2026,200 Way,100 St,09:15,TRIP-110,MED-111,VALID,Test,Passenger,Phoenix,Mesa,N")
        );
        firstPreviewResponse.EnsureSuccessStatusCode();
        secondPreviewResponse.EnsureSuccessStatusCode();
        var first = await firstPreviewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        var second = await secondPreviewResponse.Content.ReadFromJsonAsync<TripImportResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);

        using var applyFirst = await client.PostAsync($"/api/trip-imports/{first.Id}/apply", null);
        applyFirst.EnsureSuccessStatusCode();
        using var applySecond = await client.PostAsync($"/api/trip-imports/{second.Id}/apply", null);
        Assert.Equal(HttpStatusCode.Conflict, applySecond.StatusCode);
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, string csv) =>
        Upload(client, "trips.csv", System.Text.Encoding.UTF8.GetBytes(csv));

    private ApplicationDbContext CreateSeedDb() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(DatabaseConnectionString, npgsql => npgsql.UseNodaTime())
                .Options
        );

    private static Task<HttpResponseMessage> Upload(HttpClient client, string fileName, byte[] content)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(content), "file", fileName } };
        return client.PostAsync("/api/trip-imports", form);
    }

    private static string Csv(
        string row =
            "09/15/2026,200 Synthetic Way,100 Sample St,09:15,TRIP-100,MED-100,VALID,Synthetic,Passenger,Phoenix,Mesa,N"
    ) =>
        "Appointment Date,Delivery Address,Pickup Address,Time,Trip Number,Medicaid Number,Trip Status,Member's First Name,Member's Last Name,Pickup City,Delivery City,Will Call Flag\n"
        + row;

    private static byte[] Xlsx()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Trips");
        var headers = Csv().Split('\n')[0].Split(',');
        for (var index = 0; index < headers.Length; index++)
            sheet.Cell(1, index + 1).Value = headers[index];
        var row = new[]
        {
            "09/15/2026",
            "200 Way",
            "100 St",
            "09:15",
            "TRIP-105",
            "MED-105",
            "",
            "Synthetic",
            "Passenger",
            "Phoenix",
            "Mesa",
            "N",
        };
        for (var index = 0; index < row.Length; index++)
            sheet.Cell(2, index + 1).Value = row[index];
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private sealed record TripImportResponse(Guid Id, string Status, List<TripImportItemResponse> Items);

    private sealed record TripImportItemResponse(string Disposition, List<string> Messages);
}
