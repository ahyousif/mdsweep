using System.Net.Http.Json;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class DispatcherSignInTests : IAsyncLifetime
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
            builder.UseSetting("ConnectionStrings:mdsweep", database.GetConnectionString());
            builder.ConfigureServices(services =>
            {
            });
        });
    }

    [Fact]
    public async Task Anonymous_user_cannot_access_a_dispatcher_endpoint()
    {
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/api/service-days/2026-09-15/trips");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_change_a_scheduled_pickup_time()
    {
        using var client = application.CreateClient();

        using var response = await client.PutAsJsonAsync(
            "/api/trips/SYNTHETIC1/scheduled-pickup-time",
            new { ScheduledPickupTime = new TimeOnly(8, 0) }
        );

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public async Task DisposeAsync()
    {
        await application.DisposeAsync();
        await database.DisposeAsync();
    }
}
