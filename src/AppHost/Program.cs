var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("azure");

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .WithPasswordAuthentication()
    .RunAsContainer();

var database = postgres.AddDatabase("mdsweep");

var keycloakDatabase = postgres.AddDatabase("keycloak-db", databaseName: "keycloak");

var keycloakAdminPassword = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("keycloak-admin-password", "P@ssw0rd!", secret: true)
    : builder.AddParameter("keycloak-admin-password", secret: true);

var postgresUsername =
    postgres.Resource.UserName
    ?? throw new InvalidOperationException("Postgres username was not configured.");

var postgresPassword =
    postgres.Resource.Password
    ?? throw new InvalidOperationException("Postgres password was not configured.");

var keycloak = builder
    .AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2.5")
    .WithHttpEndpoint(port: 8081, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/realms/mdsweep/.well-known/openid-configuration")
    .WithReference(keycloakDatabase)
    .WaitFor(keycloakDatabase)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL_HOST", postgres.Resource.Host)
    .WithEnvironment("KC_DB_URL_PORT", postgres.Resource.Port)
    .WithEnvironment("KC_DB_URL_DATABASE", keycloakDatabase.Resource.DatabaseName)
    .WithEnvironment("KC_DB_USERNAME", postgresUsername)
    .WithEnvironment("KC_DB_PASSWORD", postgresPassword);

if (builder.ExecutionContext.IsRunMode)
{
    keycloak
        .WithBindMount("keycloak", "/opt/keycloak/data/import", isReadOnly: true)
        .WithArgs("start-dev", "--import-realm");
}
else
{
    keycloak.WithEnvironment("KC_DB_URL_PROPERTIES", "?sslmode=require").WithArgs("start");
}

var api = builder
    .AddProject<Projects.Mdsweep_Api>("api")
    .WithHttpEndpoint(port: 5080, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WithEnvironment("Authentication__Authority", "http://localhost:8081/realms/mdsweep")
    .WithEnvironment("Authentication__Audience", "mdsweep-api")
    .WaitFor(database)
    .WaitFor(keycloak);

var web = builder
    .AddViteApp("web", "../Web", runScriptName: "start")
    .WithEndpoint("http", endpoint => endpoint.Port = 4200)
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(web, "./wwwroot");

await builder.Build().RunAsync();
