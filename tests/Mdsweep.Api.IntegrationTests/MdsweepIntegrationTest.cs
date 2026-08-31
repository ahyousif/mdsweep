using System.Net.Http.Json;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public abstract class MdsweepIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    protected WebApplicationFactory<Program> Application = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        Application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:mdsweep", database.GetConnectionString());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKeycloakUserAdministration>();
                services.AddSingleton<IKeycloakUserAdministration, TestKeycloakUserAdministration>();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
            });
        });
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = TenantAggregate.Create("mdsw-eep2-3456", "Synthetic Tenant", "synthetic-tenant");
        var user = UserAggregate.Create("Synthetic", "Dispatcher", "dispatcher-test");
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, user.Id, "Dispatcher"));
        await db.SaveChangesAsync();
        var tenants = scope.ServiceProvider.GetRequiredService<IDynamicTenantSource<string>>();
        await tenants.AddTenantAsync(tenant.Id, CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await Application.DisposeAsync();
        await database.DisposeAsync();
    }

    protected static async Task AddAntiforgeryToken(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<AntiforgeryResponse>("/api/auth/antiforgery");
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", result!.Token);
    }

    protected sealed record AntiforgeryResponse(string Token);

    protected sealed class TestKeycloakUserAdministration : IKeycloakUserAdministration
    {
        public Task<string> CreateDriverAsync(string email, string temporaryPassword, string organizationId, CancellationToken cancellationToken) => Task.FromResult($"test-{email}");
        public Task ResetPasswordAsync(string subject, string temporaryPassword, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteUserAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
