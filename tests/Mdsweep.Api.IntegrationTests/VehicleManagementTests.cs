using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Api.Features.Vehicles;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class VehicleManagementTests : MdsweepIntegrationTest
{
    private const string Vin = "1M8GDM9AXKP042788";
    private const string OtherVin = "1M8GDM9AXKP042789";

    [Fact]
    public async Task Optional_vehicle_attributes_can_be_created_updated_validated_and_cleared()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        using var created = await client.PostAsJsonAsync(
            "/api/vehicles",
            new
            {
                displayLabel = "Family van",
                vin = Vin,
                year = 2022,
                make = "Toyota",
                model = "Sienna",
            }
        );
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var vehicle = (await created.Content.ReadFromJsonAsync<VehicleResponse>())!;
        Assert.Equal(2022, vehicle.Year);
        Assert.Equal("Toyota", vehicle.Make);
        Assert.Equal("Sienna", vehicle.Model);
        var fetched = (await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{vehicle.Id}"))!;
        Assert.Equal(vehicle, fetched);
        foreach (var year in new[] { 1899, DateTime.UtcNow.Year + 2 })
        {
            await Invalid(
                client.PostAsJsonAsync(
                    "/api/vehicles",
                    new
                    {
                        displayLabel = "Invalid",
                        vin = OtherVin,
                        year,
                    }
                ),
                "year"
            );
            await Invalid(
                client.PutAsJsonAsync(
                    $"/api/vehicles/{vehicle.Id}",
                    new
                    {
                        displayLabel = "Invalid",
                        vin = Vin,
                        year,
                    }
                ),
                "year"
            );
        }
        foreach (var field in new[] { "make", "model" })
        {
            var invalid = new Dictionary<string, object>
            {
                ["displayLabel"] = "Invalid",
                ["vin"] = OtherVin,
                [field] = new string('x', 101),
            };
            await Invalid(client.PostAsJsonAsync("/api/vehicles", invalid), field);
            await Invalid(client.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", invalid), field);
        }
        using var updated = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}",
            new
            {
                displayLabel = "Updated van",
                vin = Vin,
                year = 2023,
                make = "Ford",
                model = "Transit",
            }
        );
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var listed = Assert.Single((await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!);
        Assert.Equal(2023, listed.Year);
        Assert.Equal("Ford", listed.Make);
        Assert.Equal("Transit", listed.Model);
        using var cleared = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}",
            new { displayLabel = "Cleared van", vin = Vin }
        );
        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
        fetched = (await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{vehicle.Id}"))!;
        Assert.Null(fetched.Year);
        Assert.Null(fetched.Make);
        Assert.Null(fetched.Model);
    }

    [Fact]
    public async Task Dispatcher_can_manage_vehicles_without_trips()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var vehicle = await Create(client, "Van 1", Vin.ToLowerInvariant());
        Assert.Equal(Vin.ToLowerInvariant(), vehicle.Vin);
        Assert.True(vehicle.IsActive);
        Assert.Equal(7, vehicle.Id.Version);

        using var update = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}",
            new { displayLabel = "Van 2", vin = OtherVin }
        );
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        using var deactivate = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}/active",
            new { isActive = false }
        );
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        var inactive = await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{vehicle.Id}");
        Assert.Equal("Van 2", inactive!.DisplayLabel);
        Assert.Equal(OtherVin, inactive.Vin);
        Assert.False(inactive.IsActive);
        // Editing an inactive record must not silently reactivate it.
        using var editInactive = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}",
            new { displayLabel = "Spare van", vin = OtherVin }
        );
        Assert.Equal(HttpStatusCode.NoContent, editInactive.StatusCode);
        Assert.False((await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{vehicle.Id}"))!.IsActive);
        using var reactivate = await client.PutAsJsonAsync(
            $"/api/vehicles/{vehicle.Id}/active",
            new { isActive = true }
        );
        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);
        var list = await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles");
        Assert.True(Assert.Single(list!).IsActive);

        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Vehicles.IgnoreQueryFilters().SingleAsync(x => x.Id == vehicle.Id);
        Assert.Equal("mdsw-eep2-3456", stored.TenantId);
        Assert.Equal("Spare van", stored.DisplayLabel);
        Assert.Empty(await db.Trips.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Invalid_fields_are_rejected_on_create_and_update_without_changing_the_record()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var vehicle = await Create(client, "Van 1", Vin);
        string?[] invalidVins =
        [
            null,
            "",
            " ",
            "1M8GDM9AXKP04278",
            Vin + "9",
            "IM8GDM9AXKP042788",
            "OM8GDM9AXKP042788",
            "QM8GDM9AXKP042788",
            "1M8GDM9AXKP04278!",
            "١M8GDM9AXKP042788",
            Vin + "\n",
        ];
        foreach (var vin in invalidVins)
        {
            await Invalid(client.PostAsJsonAsync("/api/vehicles", new { displayLabel = "Van", vin }), "vin");
            await Invalid(
                client.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new { displayLabel = "Van", vin }),
                "vin"
            );
        }
        foreach (var displayLabel in new string?[] { null, "", "   ", new('x', 101) })
            await Invalid(
                client.PostAsJsonAsync("/api/vehicles", new { displayLabel, vin = OtherVin }),
                "displayLabel"
            );
        await Invalid(client.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new { }), "isActive");
        Assert.Equal(
            "Van 1",
            (await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{vehicle.Id}"))!.DisplayLabel
        );
        Assert.Single((await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!);
    }

    [Fact]
    public async Task Duplicate_vins_include_inactive_records_and_do_not_block_saving_the_same_vehicle()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var first = await Create(client, "Van 1", Vin);
        var second = await Create(client, "Van 2", OtherVin);
        using var deactivate = await client.PutAsJsonAsync(
            $"/api/vehicles/{first.Id}/active",
            new { isActive = false }
        );
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        await Duplicate(client.PostAsJsonAsync("/api/vehicles", new { displayLabel = "Duplicate", vin = Vin }));
        await Duplicate(
            client.PutAsJsonAsync($"/api/vehicles/{second.Id}", new { displayLabel = "Duplicate", vin = Vin })
        );
        using var same = await client.PutAsJsonAsync(
            $"/api/vehicles/{first.Id}",
            new { displayLabel = "Renamed", vin = Vin }
        );
        Assert.Equal(HttpStatusCode.NoContent, same.StatusCode);
        Assert.Equal(OtherVin, (await client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{second.Id}"))!.Vin);
    }

    [Fact]
    public async Task Concurrent_creates_leave_one_vehicle_with_database_uniqueness_enforced()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var responses = await Task.WhenAll(
            Enumerable
                .Range(0, 6)
                .Select(async index =>
                {
                    try
                    {
                        return await client.PostAsJsonAsync(
                            "/api/vehicles",
                            new { displayLabel = $"Synthetic van {index}", vin = Vin }
                        );
                    }
                    catch (DbUpdateException exception)
                        when (exception.InnerException
                                is PostgresException
                                {
                                    SqlState: PostgresErrorCodes.UniqueViolation,
                                    ConstraintName: "ix_vehicles_tenant_id_vin"
                                }
                        )
                    {
                        // TestServer propagates unhandled save failures instead of producing an HTTP 500.
                        return null;
                    }
                })
        );
        try
        {
            Assert.Single(responses, response => response?.StatusCode == HttpStatusCode.Created);
            foreach (
                var response in responses
                    .OfType<HttpResponseMessage>()
                    .Where(response => response.StatusCode != HttpStatusCode.Created)
            )
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal(
                    "vehicleVinExists",
                    json.RootElement.GetProperty("issues")[0].GetProperty("code").GetString()
                );
            }
            Assert.Single((await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!);
        }
        finally
        {
            foreach (var response in responses)
                response?.Dispose();
        }
    }

    [Fact]
    public async Task Tenant_isolation_applies_to_reads_writes_and_vin_uniqueness()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var first = await Create(client, "First tenant", Vin);
        const string secondTenant = "abcd-efgh-jkmn";
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync();
            db.Tenants.Add(TenantAggregate.Create(secondTenant, "Other Synthetic Tenant"));
            db.TenantMemberships.Add(
                TenantMembership.Create(secondTenant, user.Id, "Synthetic Dispatcher", ["Dispatcher"])
            );
            await db.SaveChangesAsync();
            await scope
                .ServiceProvider.GetRequiredService<IDynamicTenantSource<string>>()
                .AddTenantAsync(secondTenant, CancellationToken.None);
        }
        using var other = Application.CreateClient();
        other.DefaultRequestHeaders.Add("X-Test-Tenant", secondTenant);
        await AddAntiforgeryToken(other);
        Assert.Empty((await other.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!);
        using var get = await other.GetAsync($"/api/vehicles/{first.Id}");
        using var update = await other.PutAsJsonAsync(
            $"/api/vehicles/{first.Id}",
            new { displayLabel = "Forbidden", vin = OtherVin }
        );
        using var deactivate = await other.PutAsJsonAsync($"/api/vehicles/{first.Id}/active", new { isActive = false });
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deactivate.StatusCode);
        var second = await Create(other, "Second tenant", Vin);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(
            first.Id,
            Assert.Single((await client.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!).Id
        );
        Assert.Equal(
            second.Id,
            Assert.Single((await other.GetFromJsonAsync<List<VehicleResponse>>("/api/vehicles"))!).Id
        );
    }

    [Fact]
    public async Task Driver_and_inactive_members_cannot_use_any_vehicle_operation()
    {
        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);
        var vehicle = await Create(client, "Van", Vin);
        foreach (var inactive in new[] { false, true })
        {
            await using (var scope = Application.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var membership = await db.TenantMemberships.SingleAsync();
                membership.SetRoles(inactive ? ["Administrator"] : ["Driver"]);
                membership.SetActive(!inactive);
                await db.SaveChangesAsync();
            }
            using var list = await client.GetAsync("/api/vehicles");
            using var get = await client.GetAsync($"/api/vehicles/{vehicle.Id}");
            using var create = await client.PostAsJsonAsync(
                "/api/vehicles",
                new { displayLabel = "Van", vin = OtherVin }
            );
            using var update = await client.PutAsJsonAsync(
                $"/api/vehicles/{vehicle.Id}",
                new { displayLabel = "Van", vin = OtherVin }
            );
            using var status = await client.PutAsJsonAsync(
                $"/api/vehicles/{vehicle.Id}/active",
                new { isActive = false }
            );
            foreach (var response in new[] { list, get, create, update, status })
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Administrator_can_manage_vehicles_and_anonymous_or_unprotected_mutations_are_rejected()
    {
        await using (var scope = Application.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.TenantMemberships.SingleAsync()).SetRoles(["Administrator"]);
            await db.SaveChangesAsync();
        }
        using var client = Application.CreateClient();
        using var missingToken = await client.PostAsJsonAsync("/api/vehicles", new { displayLabel = "Van", vin = Vin });
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        await AddAntiforgeryToken(client);
        await Create(client, "Van", Vin);
        using var anonymous = Application.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using var denied = await anonymous.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task Migration_upgrades_the_existing_baseline_without_losing_existing_records()
    {
        // The test host starts on the latest schema. Move only this isolated test database
        // back to its baseline, then verify the supported upgrade with existing Tenant data.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureForMdsweep(DatabaseConnectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260916045136_InitialSchema");
        Assert.Single(await db.Tenants.ToListAsync());
        await migrator.MigrateAsync();
        Assert.Single(await db.Tenants.ToListAsync());
        Assert.Empty(await db.Vehicles.ToListAsync());
    }

    private static async Task<VehicleResponse> Create(HttpClient client, string displayLabel, string vin)
    {
        using var response = await client.PostAsJsonAsync("/api/vehicles", new { displayLabel, vin });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var model = await response.Content.ReadFromJsonAsync<VehicleResponse>();
        Assert.NotNull(model);
        using var get = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        return model;
    }

    private static async Task Invalid(Task<HttpResponseMessage> request, string field)
    {
        using var response = await request;
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    private static async Task Duplicate(Task<HttpResponseMessage> request)
    {
        using var response = await request;
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("vehicleVinExists", json.RootElement.GetProperty("issues")[0].GetProperty("code").GetString());
    }
}
