using Aspire.Hosting.Azure;
using Aspire.Hosting.JavaScript;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Resources;

namespace Mdsweep.AppHost;

public static class HostingExtensions
{
    public sealed record MdsweepCommunicationResources(
        IResourceBuilder<MailPitContainerResource>? Mailpit,
        IResourceBuilder<ParameterResource>? WebBaseUrl,
        IResourceBuilder<IResourceWithConnectionString>? SmtpConnection,
        IResourceBuilder<ParameterResource>? SmtpUsername,
        IResourceBuilder<ParameterResource>? SmtpPassword,
        IResourceBuilder<ParameterResource>? SmtpFrom
    );

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
        MdsweepCommunicationResources communications
    )
    {
        var oidcClientSecret = builder.ExecutionContext.IsRunMode
            ? builder.AddParameter("oidc-client-secret", "Development-only-secret", secret: true)
            : builder.AddParameter("oidc-client-secret", secret: true);

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
            .WithEnvironment("GoogleRoutes__ApiKey", googleRoutesApiKey)
            .WaitFor(database)
            .WaitFor(keycloak);

        ConfigureWebAndEmail(api, communications);

        return api;
    }

    public static IResourceBuilder<ProjectResource> AddMdsweepUtility(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzurePostgresFlexibleServerResource> postgres,
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> database,
        MdsweepCommunicationResources communications
    )
    {
        var utility = builder
            .AddProject<Projects.Mdsweep_Utility>("utility")
            .WithReference(database)
            .WaitFor(database)
            .WithExplicitStart();

        ConfigureWebAndEmail(utility, communications);

        if (!builder.ExecutionContext.IsRunMode)
        {
            utility
                .WithEnvironment(
                    "Azure__SubscriptionId",
                    builder.Configuration["Azure:SubscriptionId"]
                        ?? throw new InvalidOperationException("Azure subscription is not configured.")
                )
                .WithEnvironment(
                    "Azure__ResourceGroup",
                    builder.Configuration["Azure:ResourceGroup"]
                        ?? throw new InvalidOperationException("Azure resource group is not configured.")
                )
                .WithEnvironment("Azure__PostgresServerName", postgres.Resource.NameOutputReference);
        }

        return utility.PublishAsAzureContainerAppJob(
            (_, job) =>
            {
                job.Identity = new ManagedServiceIdentity
                {
                    ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned,
                };
                job.Configuration.TriggerType = ContainerAppJobTriggerType.Manual;
                job.Configuration.ReplicaRetryLimit = 0;
                job.Configuration.ReplicaTimeout = 600;
            }
        );
    }

    public static MdsweepCommunicationResources AddMdsweepCommunications(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<MailPitContainerResource>? mailpit
    )
    {
        if (mailpit is not null)
        {
            return new MdsweepCommunicationResources(mailpit, null, null, null, null, null);
        }

        var webBaseUrl = builder.AddParameter("web-base-url");
        var smtpConnection = builder.AddConnectionString("smtp");
        var smtpUsername = builder.AddParameter("smtp-username", secret: true);
        var smtpPassword = builder.AddParameter("smtp-password", secret: true);
        var smtpFrom = builder.AddParameter("smtp-from");

        return new MdsweepCommunicationResources(
            null,
            webBaseUrl,
            smtpConnection,
            smtpUsername,
            smtpPassword,
            smtpFrom
        );
    }

    private static void ConfigureWebAndEmail(
        IResourceBuilder<ProjectResource> resource,
        MdsweepCommunicationResources communications
    )
    {
        if (communications.Mailpit is not null)
        {
            resource.WithReference(communications.Mailpit).WaitFor(communications.Mailpit);
            return;
        }

        resource
            .WithEnvironment("Web__BaseUrl", communications.WebBaseUrl!)
            .WithReference(communications.SmtpConnection!)
            .WithEnvironment("Email__ConnectionStringName", "smtp")
            .WithEnvironment("Email__Username", communications.SmtpUsername!)
            .WithEnvironment("Email__Password", communications.SmtpPassword!)
            .WithEnvironment("Email__From", communications.SmtpFrom!)
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
