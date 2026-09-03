var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("azure");

var postgres = builder.AddAzurePostgresFlexibleServer("postgres");

if (builder.ExecutionContext.IsRunMode)
{
    postgres.WithPasswordAuthentication().RunAsContainer();
}
else
{
    var keyVault = builder.AddAzureKeyVault("mdsweep-kv");
    postgres.WithPasswordAuthentication(keyVault);
}

var database = postgres.AddDatabase("mdsweep");

var keycloakDatabase = postgres.AddDatabase("keycloak-db", databaseName: "keycloak");

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
    var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", "P@ssw0rd!", secret: true);

    keycloak
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
        .WithBindMount("keycloak", "/opt/keycloak/data/import", isReadOnly: true)
        .WithArgs("start-dev", "--import-realm");
}
else
{
    keycloak
        .WithEnvironment("KC_DB_URL_PROPERTIES", "?sslmode=require")
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithArgs("start");
}

var oidcClientSecret = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("oidc-client-secret", "Development-only-secret", secret: true)
    : builder.AddParameter("oidc-client-secret", secret: true);

var administrationClientSecret = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("administration-client-secret", "Development-only-administration-secret", secret: true)
    : builder.AddParameter("administration-client-secret", secret: true);

var keycloakAuthority = builder.ExecutionContext.IsRunMode
    ? ReferenceExpression.Create(
        $"{keycloak.GetEndpoint("http", KnownNetworkIdentifiers.LocalhostNetwork)}/realms/mdsweep"
    )
    : ReferenceExpression.Create(
        $"{keycloak.GetEndpoint("http", KnownNetworkIdentifiers.PublicInternet)}/realms/mdsweep"
    );

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
    .WaitFor(database)
    .WaitFor(keycloak);

var web = builder
    .AddViteApp("web", "../Mdsweep.Web", runScriptName: "start")
    .WithEndpoint("http", endpoint => endpoint.Port = 4200)
    .WithReference(api)
    .WaitFor(api)
    .ClearContainerFilesSources()
    .WithContainerFilesSource("/app/dist/web/browser");

api.PublishWithContainerFiles(web, "./wwwroot");

await builder.Build().RunAsync();
