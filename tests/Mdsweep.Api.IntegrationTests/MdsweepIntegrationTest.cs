using System.Net.Http.Json;
using Mdsweep.Application.Users;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
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
            builder.UseSetting("GoogleRoutes:ApiKey", "synthetic-google-routes-api-key");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIdentityAdministration>();
                services.AddSingleton<TestKeycloakUserAdministration>();
                services.AddSingleton<IIdentityAdministration>(sp =>
                    sp.GetRequiredService<TestKeycloakUserAdministration>()
                );
                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, DispatcherAuthenticationHandler>("Test", _ => { });
                services.PostConfigure<OpenIdConnectOptions>(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        var configuration = new OpenIdConnectConfiguration
                        {
                            AuthorizationEndpoint = "https://keycloak.test/authorize",
                            TokenEndpoint = "https://keycloak.test/token",
                            EndSessionEndpoint = "https://keycloak.test/logout",
                        };
                        options.Configuration = configuration;
                        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                            configuration
                        );
                    }
                );
                ConfigureTestServices(services);
            });
        });
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = TenantAggregate.Create("mdsw-eep2-3456", "Synthetic Tenant", "synthetic-tenant");
        var user = UserAggregate.Create("Synthetic", "Dispatcher", "dispatcher-test", "dispatcher@example.test");
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

    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    public sealed class TestKeycloakUserAdministration : IIdentityAdministration
    {
        public bool FailEmail { get; set; }
        public bool IsMember { get; set; } = true;
        public bool Verified { get; set; } = true;
        public string Email { get; set; } = "driver@example.test";
        public string? LastInvitationToken { get; private set; }

        public Task InviteAsync(
            string organizationId,
            string email,
            string firstName,
            string lastName,
            string invitationToken,
            CancellationToken ct
        )
        {
            LastInvitationToken = invitationToken;
            return FailEmail
                ? Task.FromException(
                    new IdentityAdministrationException("Email delivery is not configured. Configure it and retry.")
                )
                : Task.CompletedTask;
        }

        public Task<VerifiedIdentity?> GetVerifiedIdentityAsync(string subject, CancellationToken ct) =>
            Task.FromResult(Verified ? new VerifiedIdentity(subject, Email) : null);

        public Task<bool> IsOrganizationMemberAsync(string subject, string organizationId, CancellationToken ct) =>
            Task.FromResult(IsMember);

        public Task SendPasswordResetAsync(string subject, CancellationToken ct) =>
            FailEmail
                ? Task.FromException(
                    new IdentityAdministrationException("Email delivery is not configured. Configure it and retry.")
                )
                : Task.CompletedTask;
    }
}
