using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mdsweep.Api.Infrastructure;
using Testcontainers.PostgreSql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class DispatcherSignInTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    private WebApplicationFactory<Program> application = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BootstrapDispatcher:Email"] = "dispatcher@example.test",
                    ["BootstrapDispatcher:Password"] = "Test-only-password-42!"
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(database.GetConnectionString()));
            });
        });
    }

    [Fact]
    public async Task Dispatcher_can_sign_in_and_receive_secure_cookie()
    {
        using var client = application.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "dispatcher@example.test",
            Password = "Test-only-password-42!"
        });

        response.EnsureSuccessStatusCode();
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith(".Mdsweep.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    public async Task DisposeAsync()
    {
        await application.DisposeAsync();
        await database.DisposeAsync();
    }
}
