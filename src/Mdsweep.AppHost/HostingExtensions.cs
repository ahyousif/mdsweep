using Aspire.Hosting.Azure;
using Aspire.Hosting.JavaScript;

namespace Mdsweep.AppHost;

public static class HostingExtensions
{
    public static IResourceBuilder<AzurePostgresFlexibleServerResource> AddMdsweepPostgres(
        this IDistributedApplicationBuilder builder
    )
    {
        var postgres = builder.AddAzurePostgresFlexibleServer("postgres").WithPasswordAuthentication();

        if (builder.ExecutionContext.IsRunMode)
        {
            postgres.RunAsContainer();
        }

        return postgres;
    }

    public static IResourceBuilder<ContainerResource> AddMdsweepKeycloak(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzurePostgresFlexibleServerResource> postgres,
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> keycloakDatabase
    )
    {
        var postgresUsername =
            postgres.Resource.UserName ?? throw new InvalidOperationException("Postgres username was not configured.");

        var postgresPassword =
            postgres.Resource.Password ?? throw new InvalidOperationException("Postgres password was not configured.");

        var keycloak = builder
            .AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2.5")
            .WithHttpEndpoint(port: 8081, targetPort: 8080, name: "http")
            .WithExternalHttpEndpoints()
            .WithReference(keycloakDatabase)
            .WaitFor(keycloakDatabase)
            .WithEnvironment("KC_DB", "postgres")
            .WithEnvironment("KC_DB_URL_HOST", postgres.Resource.Host)
            .WithEnvironment("KC_DB_URL_PORT", postgres.Resource.Port)
            .WithEnvironment("KC_DB_URL_DATABASE", keycloakDatabase.Resource.DatabaseName)
            .WithEnvironment("KC_DB_USERNAME", postgresUsername)
            .WithEnvironment("KC_DB_PASSWORD", postgresPassword);

        keycloak.WithHttpHealthCheck(
            () => keycloak.GetEndpoint("http"),
            path: "/realms/master/.well-known/openid-configuration"
        );

        if (builder.ExecutionContext.IsRunMode)
        {
            ConfigureDevelopmentKeycloak(builder, keycloak);
        }
        else
        {
            ConfigureProductionKeycloak(keycloak);
        }

        return keycloak;
    }

    public static IResourceBuilder<MailPitContainerResource> AddMdsweepMailpit(
        this IDistributedApplicationBuilder builder
    )
    {
        return builder
            .AddMailPit("mailpit")
            .WithUrlForEndpoint(
                "http",
                url =>
                {
                    url.DisplayText = "Mailpit";
                    url.Url = "/";
                }
            );
    }

    public static IResourceBuilder<ProjectResource> AddMdsweepApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> database,
        IResourceBuilder<ContainerResource> keycloak,
        IResourceBuilder<MailPitContainerResource>? mailpit
    )
    {
        var oidcClientSecret = builder.ExecutionContext.IsRunMode
            ? builder.AddParameter("oidc-client-secret", "Development-only-secret", secret: true)
            : builder.AddParameter("oidc-client-secret", secret: true);

        var administrationClientSecret = builder.ExecutionContext.IsRunMode
            ? builder.AddParameter(
                "administration-client-secret",
                "Development-only-administration-secret",
                secret: true
            )
            : builder.AddParameter("administration-client-secret", secret: true);

        var googleRoutesApiKey = builder.AddParameter("google-routes-api-key", secret: true);

        var keycloakAuthority = GetKeycloakAuthority(builder, keycloak);

        var api = builder
            .AddProject<Projects.Mdsweep_Api>("api")
            .WithHttpEndpoint(port: 5080, name: "http")
            .WithExternalHttpEndpoints()
            .WithReference(database)
            .WithEnvironment("Authentication__Authority", keycloakAuthority)
            .WithEnvironment("Authentication__ClientId", "mdsweep-server")
            .WithEnvironment("Authentication__ClientSecret", oidcClientSecret)
            .WithEnvironment("KeycloakAdministration__ClientId", "mdsweep-administration")
            .WithEnvironment("KeycloakAdministration__ClientSecret", administrationClientSecret)
            .WithEnvironment("GoogleRoutes__ApiKey", googleRoutesApiKey)
            .WaitFor(database)
            .WaitFor(keycloak);

        if (mailpit is not null)
        {
            api.WithReference(mailpit).WaitFor(mailpit);
        }
        else
        {
            ConfigureProductionWebAndEmail(builder, api);
        }

        return api;
    }

    private static void ConfigureProductionWebAndEmail(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api
    )
    {
        var webBaseUrl = builder.AddParameter("web-base-url");
        var smtpConnection = builder.AddConnectionString("smtp");
        var smtpUsername = builder.AddParameter("smtp-username", secret: true);
        var smtpPassword = builder.AddParameter("smtp-password", secret: true);
        var smtpFrom = builder.AddParameter("smtp-from");

        api.WithEnvironment("Web__BaseUrl", webBaseUrl)
            .WithReference(smtpConnection)
            .WithEnvironment("Email__ConnectionStringName", "smtp")
            .WithEnvironment("Email__Username", smtpUsername)
            .WithEnvironment("Email__Password", smtpPassword)
            .WithEnvironment("Email__From", smtpFrom)
            .WithEnvironment("Email__UseStartTls", "true");
    }

    public static IResourceBuilder<ViteAppResource> AddMdsweepWeb(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api
    )
    {
        return builder
            .AddViteApp("web", "../Mdsweep.Web", runScriptName: "start")
            .WithEndpoint("http", endpoint => endpoint.Port = 4200)
            .WithReference(api)
            .WaitFor(api)
            .ClearContainerFilesSources()
            .WithContainerFilesSource("/app/dist/web/browser");
    }

    private static void ConfigureDevelopmentKeycloak(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ContainerResource> keycloak
    )
    {
        var adminPassword = builder.AddParameter("keycloak-admin-password", "P@ssw0rd!", secret: true);

        keycloak
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", adminPassword)
            .WithBindMount("keycloak", "/opt/keycloak/data/import", isReadOnly: true)
            .WithArgs("start-dev", "--import-realm");
    }

    private static void ConfigureProductionKeycloak(IResourceBuilder<ContainerResource> keycloak)
    {
        keycloak
            .WithEnvironment("KC_DB_URL_PROPERTIES", "?sslmode=require")
            .WithEnvironment("KC_HTTP_ENABLED", "true")
            .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
            .WithEnvironment("KC_HOSTNAME_STRICT", "false")
            .WithArgs("start");
    }

    private static ReferenceExpression GetKeycloakAuthority(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ContainerResource> keycloak
    )
    {
        var network = builder.ExecutionContext.IsRunMode
            ? KnownNetworkIdentifiers.LocalhostNetwork
            : KnownNetworkIdentifiers.PublicInternet;

        return ReferenceExpression.Create($"{keycloak.GetEndpoint("http", network)}/realms/mdsweep");
    }
}
