using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.DriverWork;
using Mdsweep.Domain.Identity;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class DriverWorkTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Driver_records_ordered_trip_events_with_signature_status_and_distinct_timestamps()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);

        using var dispatcherResponse = await client.GetAsync("/api/driver-work/trips");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, dispatcherResponse.StatusCode);

        await AssignTripToAuthenticatedDriver(client, "DRIVERFLOW1");
        var assigned = await client.GetFromJsonAsync<List<DriverTripResponse>>(
            "/api/driver-work/trips"
        );
        var trip = Assert.Single(assigned!);
        Assert.Equal("DRIVERFLOW1", trip.TripNumber);
        Assert.Equal("ArrivedAtPickup", trip.NextAction);
        Assert.DoesNotContain(
            "brokerStatus",
            await (await client.GetAsync("/api/driver-work/trips")).Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );

        var capturedAt = new DateTimeOffset(2026, 9, 15, 8, 45, 0, TimeSpan.Zero);
        using var invalid = await client.PostAsJsonAsync(
            "/api/driver-work/trips/DRIVERFLOW1/events",
            new
            {
                type = "PickedUp",
                deviceCapturedAt = capturedAt,
                tripLogSigned = (bool?)null,
                outcomeReason = (string?)null,
                note = (string?)null,
            }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains(
            "Arrived at Pickup",
            await invalid.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        foreach (
            var (type, index) in new[] { "ArrivedAtPickup", "PickedUp", "ArrivedAtDropOff" }.Select(
                (type, index) => (type, index)
            )
        )
        {
            using var response = await client.PostAsJsonAsync(
                "/api/driver-work/trips/DRIVERFLOW1/events",
                new
                {
                    type,
                    deviceCapturedAt = capturedAt.AddMinutes(index),
                    tripLogSigned = (bool?)null,
                    outcomeReason = (string?)null,
                    note = (string?)null,
                }
            );
            response.EnsureSuccessStatusCode();
            if (type == "ArrivedAtPickup")
            {
                using var retry = await client.PostAsJsonAsync(
                    "/api/driver-work/trips/DRIVERFLOW1/events",
                    new
                    {
                        type,
                        deviceCapturedAt = capturedAt.AddMinutes(index),
                        tripLogSigned = (bool?)null,
                        outcomeReason = (string?)null,
                        note = (string?)null,
                    }
                );
                retry.EnsureSuccessStatusCode();
                Assert.Equal(System.Net.HttpStatusCode.OK, retry.StatusCode);
                using var reversed = await client.PostAsJsonAsync(
                    "/api/driver-work/trips/DRIVERFLOW1/events",
                    new
                    {
                        type = "PickedUp",
                        deviceCapturedAt = capturedAt.AddMinutes(-1),
                        tripLogSigned = (bool?)null,
                        outcomeReason = (string?)null,
                        note = (string?)null,
                    }
                );
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, reversed.StatusCode);
            }
        }

        using var missingSignature = await client.PostAsJsonAsync(
            "/api/driver-work/trips/DRIVERFLOW1/events",
            new
            {
                type = "DroppedOff",
                deviceCapturedAt = capturedAt,
                tripLogSigned = (bool?)null,
                outcomeReason = (string?)null,
                note = (string?)null,
            }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingSignature.StatusCode);

        using var completed = await client.PostAsJsonAsync(
            "/api/driver-work/trips/DRIVERFLOW1/events",
            new
            {
                type = "DroppedOff",
                deviceCapturedAt = capturedAt.AddMinutes(3),
                tripLogSigned = false,
                outcomeReason = (string?)null,
                note = (string?)null,
            }
        );
        completed.EnsureSuccessStatusCode();
        var history = await client.GetFromJsonAsync<List<DriverTripEventResponse>>(
            "/api/driver-work/trips/DRIVERFLOW1/history"
        );
        Assert.Equal(
            ["ArrivedAtPickup", "PickedUp", "ArrivedAtDropOff", "DroppedOff"],
            history!.Select(x => x.Type)
        );
        Assert.Equal(
            [
                capturedAt,
                capturedAt.AddMinutes(1),
                capturedAt.AddMinutes(2),
                capturedAt.AddMinutes(3),
            ],
            history!.Select(x => x.DeviceCapturedAt)
        );
        Assert.All(history!, item => Assert.NotEqual(item.DeviceCapturedAt, item.ReceivedAt));
        Assert.False(history![^1].TripLogSigned);
    }

    [Fact]
    public async Task Driver_must_choose_a_standard_reason_to_record_could_not_complete()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "OUTCOME1");

        using var invalid = await client.PostAsJsonAsync(
            "/api/driver-work/trips/OUTCOME1/events",
            new
            {
                type = "CouldNotComplete",
                deviceCapturedAt = DateTimeOffset.UtcNow,
                tripLogSigned = (bool?)null,
                outcomeReason = "Unknown",
                note = (string?)null,
            }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);

        using var result = await client.PostAsJsonAsync(
            "/api/driver-work/trips/OUTCOME1/events",
            new
            {
                type = "CouldNotComplete",
                deviceCapturedAt = DateTimeOffset.UtcNow,
                tripLogSigned = (bool?)null,
                outcomeReason = "PassengerNoShow",
                note = "Waited at pickup.",
            }
        );
        result.EnsureSuccessStatusCode();
        var history = await client.GetFromJsonAsync<List<DriverTripEventResponse>>(
            "/api/driver-work/trips/OUTCOME1/history"
        );
        var outcome = Assert.Single(history!);
        Assert.Equal("CouldNotComplete", outcome.Type);
        Assert.Equal("PassengerNoShow", outcome.OutcomeReason);
        Assert.Equal("Waited at pickup.", outcome.Note);
    }

    [Fact]
    public async Task Queued_driver_action_conflict_is_retained_when_the_trip_was_reassigned()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "SYNC1");
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.TripAssignments.SingleAsync()).SupersededAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var actionId = Guid.CreateVersion7();
        var queued = new
        {
            actionId,
            tripNumber = "SYNC1",
            @event = new
            {
                type = "ArrivedAtPickup",
                deviceCapturedAt = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
                tripLogSigned = (bool?)null,
                outcomeReason = (string?)null,
                note = (string?)null,
            },
        };
        using var response = await client.PostAsJsonAsync("/api/driver-work/events/sync", queued);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        using var retry = await client.PostAsJsonAsync("/api/driver-work/events/sync", queued);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, retry.StatusCode);
        await using var conflictScope = Application.Services.CreateAsyncScope();
        var conflictDb = conflictScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await conflictDb.DriverTripSyncConflicts.CountAsync(x =>
                x.TripNumber == "SYNC1" && x.Reason.Contains("no longer assigned")
            )
        );
    }

    [Fact]
    public async Task Driver_correction_retains_the_original_event_and_requires_a_reason()
    {
        using var client = Application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        await AddAntiforgeryToken(client);
        await AssignTripToAuthenticatedDriver(client, "CORRECT1");
        var capturedAt = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);
        using var recorded = await client.PostAsJsonAsync(
            "/api/driver-work/trips/CORRECT1/events",
            new
            {
                type = "ArrivedAtPickup",
                deviceCapturedAt = capturedAt,
                tripLogSigned = (bool?)null,
                outcomeReason = (string?)null,
                note = (string?)null,
            }
        );
        recorded.EnsureSuccessStatusCode();
        Guid eventId;
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            eventId = await db.DriverTripEvents.Select(x => x.Id).SingleAsync();
        }
        using var invalid = await client.PostAsJsonAsync(
            $"/api/driver-work/trips/CORRECT1/events/{eventId}/corrections",
            new { deviceCapturedAt = capturedAt.AddMinutes(-5), reason = "" }
        );
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
        using var correction = await client.PostAsJsonAsync(
            $"/api/driver-work/trips/CORRECT1/events/{eventId}/corrections",
            new
            {
                deviceCapturedAt = capturedAt.AddMinutes(-5),
                reason = "Driver entered the wrong minute.",
            }
        );
        correction.EnsureSuccessStatusCode();
        await using var correctionScope = Application.Services.CreateAsyncScope();
        var correctionDb =
            correctionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await correctionDb.DriverTripEvents.CountAsync());
        Assert.True(
            await correctionDb.DriverTripEventCorrections.AnyAsync(x =>
                x.DriverTripEventId == eventId && x.Reason == "Driver entered the wrong minute."
            )
        );
    }
}
