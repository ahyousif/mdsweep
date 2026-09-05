using System.Net.Http.Json;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Mdsweep.Api.IntegrationTests;

public abstract class MdsweepIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    protected WebApplicationFactory<Program> Application = null!;
    protected string DatabaseConnectionString => database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        Application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:mdsweep", database.GetConnectionString());
            builder.UseSetting("Authentication:Authority", "https://keycloak.test/realms/mdsweep");
            builder.UseSetting("Authentication:ClientId", "mdsweep-test");
            builder.UseSetting("Authentication:ClientSecret", "test-secret");
            builder.UseSetting("KeycloakAdministration:ClientId", "mdsweep-administration-test");
            builder.UseSetting("KeycloakAdministration:ClientSecret", "test-administration-secret");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKeycloakUserAdministration>();
                services.AddSingleton<IKeycloakUserAdministration, TestKeycloakUserAdministration>();
                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
                services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        AuthorizationEndpoint = "https://keycloak.test/authorize",
                        TokenEndpoint = "https://keycloak.test/token",
                        EndSessionEndpoint = "https://keycloak.test/logout",
                    };
                });
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
        public Task<string> CreateDriverAsync(
            string email,
            string temporaryPassword,
            string organizationId,
            CancellationToken cancellationToken
        ) => Task.FromResult($"test-{email}");

        public Task ResetPasswordAsync(string subject, string temporaryPassword, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteUserAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
