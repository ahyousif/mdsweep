using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ManifestImportTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Preview_reports_exceptions_without_persisting_trips()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await using var file = File.OpenRead(FixturePath("mtm-manifest.csv"));
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", "mtm-manifest.csv" },
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
        Assert.False(preview.Rows.Single(x => x.TripNumber == "SYNTH200A").IsActive);

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.Trips.CountAsync());
    }

    [Fact]
    public async Task Angular_xsrf_cookie_authorizes_manifest_upload()
    {
        using var client = Application.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false }
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        using var tokenResponse = await client.GetAsync("/api/auth/antiforgery");
        tokenResponse.EnsureSuccessStatusCode();
        var cookies = tokenResponse
            .Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .ToArray();
        var requestToken = cookies.Single(cookie =>
            cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal)
        )["XSRF-TOKEN=".Length..];
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", requestToken);
        using var form = new MultipartFormDataContent
        {
            {
                new StringContent(
                    Manifest(Row("XSRF100A", "VALID", "0915", "100 First St", "200 Main St"))
                ),
                "file",
                "manifest.csv"
            },
        };

        using var response = await client.PostAsync("/api/manifest-imports/preview", form);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Excel_manifest_uses_the_same_preview_and_apply_workflow_as_csv()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Manifest");
        var headers = new[]
        {
            "Appointment Date",
            "Delivery Address",
            "Pickup Address",
            "Time",
            "Trip Number",
            "Trip Status",
            "Member's First Name",
            "Member's Last Name",
            "Pickup City",
            "Delivery City",
            "Passenger Type",
            "Vehicle Type",
            "Will Call Flag",
        };
        for (var column = 0; column < headers.Length; column++)
            worksheet.Cell(1, column + 1).Value = headers[column];

        worksheet.Cell(2, 1).Value = new DateTime(2026, 9, 15);
        worksheet.Cell(2, 2).Value = "200 Main St";
        worksheet.Cell(2, 3).Value = "100 First St";
        worksheet.Cell(2, 4).Value = new TimeSpan(9, 15, 0);
        worksheet.Cell(2, 5).Value = "EXCEL100A";
        worksheet.Cell(2, 6).Value = "VALID";
        worksheet.Cell(2, 7).Value = "Synthetic";
        worksheet.Cell(2, 8).Value = "Passenger";
        worksheet.Cell(2, 9).Value = "Phoenix";
        worksheet.Cell(2, 10).Value = "Mesa";
        worksheet.Cell(2, 11).Value = "Ambulatory";
        worksheet.Cell(2, 12).Value = "Cab";
        worksheet.Cell(2, 13).Value = "N";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        await AddAntiforgeryToken(client);
        using var form = new MultipartFormDataContent
        {
            { new StreamContent(stream), "file", "mtm-manifest.xlsx" },
        };

        using var response = await client.PostAsync("/api/manifest-imports/preview", form);

        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(preview);
        var row = Assert.Single(preview.Rows);
        Assert.Equal("EXCEL100A", row.TripNumber);
        Assert.Equal("Ready", row.Disposition);

        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();
        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        var trip = Assert.Single(serviceDay!);
        Assert.Equal("EXCEL100A", trip.TripNumber);
        Assert.Equal(new TimeOnly(9, 15), trip.AppointmentTime);
    }

    [Fact]
    public async Task Manifest_preview_rejects_legacy_or_invalid_excel_files_with_actionable_errors()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);

        using var legacyForm = new MultipartFormDataContent
        {
            { new StringContent("not an excel workbook"), "file", "mtm-manifest.xls" },
        };
        using var legacyResponse = await client.PostAsync(
            "/api/manifest-imports/preview",
            legacyForm
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, legacyResponse.StatusCode);
        Assert.Contains(
            ".xlsx",
            await legacyResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );

        using var invalidForm = new MultipartFormDataContent
        {
            { new StringContent("not an excel workbook"), "file", "mtm-manifest.xlsx" },
        };
        using var invalidResponse = await client.PostAsync(
            "/api/manifest-imports/preview",
            invalidForm
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Contains(
            "could not be read",
            await invalidResponse.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task Applying_preview_imports_trips_groups_journeys_and_retains_blocked_rows()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await Preview(client, "mtm-manifest.csv");

        using var applyResponse = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );

        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.NotNull(applied);
        Assert.Equal(3, applied.Imported);
        Assert.Equal(1, applied.Blocked);

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        Assert.NotNull(serviceDay);
        Assert.Equal(3, serviceDay.Count);
        Assert.Equal(2, serviceDay.Count(x => x.JourneyKey == "SYNTH100"));
        Assert.Contains(serviceDay, x => x.TripNumber == "SYNTH200A" && !x.IsActive);

        var retainedPreview = await client.GetFromJsonAsync<PreviewResponse>(
            $"/api/manifest-imports/{preview.PreviewId}"
        );
        Assert.NotNull(retainedPreview);
        Assert.Contains(
            retainedPreview.Rows,
            x => x.TripNumber == "SYNTH300A" && x.Disposition == "Blocked"
        );

        using var retryResponse = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        retryResponse.EnsureSuccessStatusCode();
        var afterRetry = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        Assert.Equal(3, afterRetry!.Count);
    }

    [Fact]
    public async Task Wolverine_unit_of_work_rolls_back_all_manifest_changes_when_save_fails()
    {
        var tenantId = "mdsw-eep2-3456";
        var duplicate = new ManifestPreviewRow(
            "ROLLBACK100A",
            ManifestRowDisposition.Ready,
            [],
            new DateOnly(2026, 9, 15),
            new TimeOnly(9, 15),
            "Synthetic",
            "Rider",
            "100 First St",
            "Phoenix",
            "200 Main St",
            "Mesa",
            null,
            "Ambulatory",
            "Cab",
            "VALID",
            false
        );
        var preview = new ManifestPreview
        {
            TenantId = tenantId,
            FileName = "rollback.csv",
            RowsJson = JsonSerializer.Serialize(new[] { duplicate, duplicate }),
        };

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            setupDb.ManifestPreviews.Add(preview);
            await setupDb.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () =>
            response = await client.PostAsync($"/api/manifest-imports/{preview.Id}/apply", null)
        );

        if (failure is null)
        {
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response!.StatusCode);
        }
        response?.Dispose();

        await using var assertionScope = Application.Services.CreateAsyncScope();
        var db = assertionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Trips.AnyAsync(x => x.TripNumber == duplicate.TripNumber));
        Assert.False(await db.TripBrokerImports.AnyAsync(x => x.ManifestPreviewId == preview.Id));
        Assert.Null((await db.ManifestPreviews.SingleAsync(x => x.Id == preview.Id)).AppliedAt);
    }

    [Fact]
    public async Task Preview_blocks_every_row_with_a_duplicate_trip_number()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var preview = await PreviewCsv(
            client,
            Manifest(
                Row("DUPLICATE1", "VALID", "0915", "100 First St", "200 Main St"),
                Row("DUPLICATE1", "VALID", "1015", "300 Second St", "400 Oak St")
            )
        );

        Assert.Equal(0, preview.Ready);
        Assert.Equal(0, preview.Warning);
        Assert.Equal(2, preview.Blocked);
        Assert.All(
            preview.Rows,
            row =>
                Assert.Contains(
                    row.Messages,
                    message =>
                        message.Contains("more than once", StringComparison.OrdinalIgnoreCase)
                )
        );

        using var applyResponse = await client.PostAsync(
            $"/api/manifest-imports/{preview.PreviewId}/apply",
            null
        );
        applyResponse.EnsureSuccessStatusCode();
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.Equal(0, applied!.Imported);
    }

    [Fact]
    public async Task Revised_manifest_reconciles_broker_fields_and_keeps_import_history()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var original = await PreviewCsv(
            client,
            Manifest(Row("REVISED1", "VALID", "0915", "100 First St", "200 Main St"))
        );
        using var firstApply = await client.PostAsync(
            $"/api/manifest-imports/{original.PreviewId}/apply",
            null
        );
        firstApply.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(
            client,
            Manifest(Row("REVISED1", "TURN BACK", "1030", "300 New St", "400 Changed St"))
        );
        using var secondApply = await client.PostAsync(
            $"/api/manifest-imports/{revised.PreviewId}/apply",
            null
        );
        secondApply.EnsureSuccessStatusCode();

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        var trip = Assert.Single(serviceDay!);
        Assert.Equal("TURN BACK", trip.BrokerStatus);
        Assert.Equal(new TimeOnly(10, 30), trip.AppointmentTime);
        Assert.Equal("300 New St", trip.PickupAddress);
        Assert.False(trip.IsActive);

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var history = await db.TripBrokerImports.OrderBy(x => x.ImportedAt).ToListAsync();
        Assert.Equal(["VALID", "TURN BACK"], history.Select(x => x.BrokerStatus));
        Assert.Equal(["100 First St", "300 New St"], history.Select(x => x.PickupAddress));
    }

    [Fact]
    public async Task Repeat_import_preview_identifies_unchanged_and_broker_changed_trips_without_applying_them()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var originalCsv = Manifest(
            Row("SAME1", "VALID", "0915", "100 First St", "200 Main St"),
            Row("CHANGED1", "VALID", "1015", "300 Second St", "400 Oak St")
        );
        var original = await PreviewCsv(client, originalCsv);
        using var apply = await client.PostAsync(
            $"/api/manifest-imports/{original.PreviewId}/apply",
            null
        );
        apply.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(
            client,
            Manifest(
                Row("SAME1", "VALID", "0915", "100 First St", "200 Main St"),
                Row("CHANGED1", "TURN BACK", "1030", "500 Revised St", "600 Changed St"),
                Row("NEW1", "VALID", "1100", "700 New St", "800 Newer St")
            )
        );

        Assert.Equal("Unchanged", revised.Rows.Single(x => x.TripNumber == "SAME1").BrokerChange);
        var changed = revised.Rows.Single(x => x.TripNumber == "CHANGED1");
        Assert.Equal("BrokerChanged", changed.BrokerChange);
        Assert.Contains(
            changed.Messages,
            message =>
                message.Contains("appointment time", StringComparison.OrdinalIgnoreCase)
                && message.Contains("pickup address", StringComparison.OrdinalIgnoreCase)
                && message.Contains("MTM status", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Equal("New", revised.Rows.Single(x => x.TripNumber == "NEW1").BrokerChange);

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        Assert.Equal("VALID", serviceDay!.Single(x => x.TripNumber == "CHANGED1").BrokerStatus);
        Assert.DoesNotContain(serviceDay!, x => x.TripNumber == "NEW1");
    }

    [Fact]
    public async Task Revised_manifest_preserves_provider_scheduled_pickup_and_its_history()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        var original = await PreviewCsv(
            client,
            Manifest(Row("SCHEDULED1", "VALID", "0915", "100 First St", "200 Main St"))
        );
        using var firstApply = await client.PostAsync(
            $"/api/manifest-imports/{original.PreviewId}/apply",
            null
        );
        firstApply.EnsureSuccessStatusCode();

        using var firstSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 0) }
        );
        firstSchedule.EnsureSuccessStatusCode();

        var unchangedWithProviderSchedule = await PreviewCsv(
            client,
            Manifest(Row("SCHEDULED1", "VALID", "0915", "100 First St", "200 Main St"))
        );
        var unchanged = Assert.Single(unchangedWithProviderSchedule.Rows);
        Assert.Equal("Unchanged", unchanged.BrokerChange);
        Assert.True(unchanged.HasProviderOverrides);
        Assert.True(unchanged.IsActive);
        Assert.Contains(
            unchanged.Messages,
            message =>
                message.Contains(
                    "scheduled pickup time will be preserved",
                    StringComparison.OrdinalIgnoreCase
                )
        );

        using var replacementSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 10) }
        );
        replacementSchedule.EnsureSuccessStatusCode();
        using var retriedSchedule = await client.PutAsJsonAsync(
            "/api/trips/SCHEDULED1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 10) }
        );
        retriedSchedule.EnsureSuccessStatusCode();

        var revised = await PreviewCsv(
            client,
            Manifest(Row("SCHEDULED1", "VALID", "1030", "300 Revised St", "400 Changed St"))
        );
        var changed = Assert.Single(revised.Rows);
        Assert.Equal("BrokerChanged", changed.BrokerChange);
        Assert.True(changed.HasProviderOverrides);
        Assert.True(changed.IsActive);
        Assert.Contains(
            changed.Messages,
            message =>
                message.Contains(
                    "scheduled pickup time will be preserved",
                    StringComparison.OrdinalIgnoreCase
                )
        );

        using var revisedApply = await client.PostAsync(
            $"/api/manifest-imports/{revised.PreviewId}/apply",
            null
        );
        revisedApply.EnsureSuccessStatusCode();

        var serviceDay = await client.GetFromJsonAsync<List<ServiceDayTrip>>(
            "/api/service-days/2026-09-15/trips"
        );
        var trip = Assert.Single(serviceDay!);
        Assert.Equal(new TimeOnly(10, 30), trip.AppointmentTime);
        Assert.Equal(new TimeOnly(8, 10), trip.ScheduledPickupTime);

        var history = await client.GetFromJsonAsync<List<ScheduledPickupChange>>(
            "/api/trips/SCHEDULED1/scheduled-pickup-time/history"
        );
        Assert.Equal(
            [new TimeOnly(8, 0), new TimeOnly(8, 10)],
            history!.Select(x => x.ScheduledPickupTime)
        );
        Assert.Equal([1L, 2L], history!.Select(x => x.Sequence));
        Assert.All(history!, change => Assert.True(Guid.TryParse(change.ChangedBy, out _)));
    }
}
