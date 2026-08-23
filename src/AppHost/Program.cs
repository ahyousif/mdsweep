var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("mdsweep");
var keycloakPassword =
    builder.Environment.EnvironmentName == "Development"
        ? builder.AddParameter("keycloak-admin-password", "Development-only-password", secret: true)
        : builder.AddParameter("keycloak-admin-password", secret: true);

var keycloak = builder
    .AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2.5")
    .WithHttpEndpoint(port: 8081, targetPort: 8080, name: "http")
    .WithHttpHealthCheck("/realms/mdsweep/.well-known/openid-configuration")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakPassword)
    .WithBindMount("keycloak", "/opt/keycloak/data/import", isReadOnly: true)
    .WithArgs("start-dev", "--import-realm");

var api = builder
    .AddProject<Projects.Mdsweep_Api>("api")
    .WithHttpEndpoint(port: 5080, name: "http")
    .WithReference(database)
    .WithEnvironment("Authentication__Authority", "http://localhost:8081/realms/mdsweep")
    .WithEnvironment("Authentication__Audience", "mdsweep-api")
    .WaitFor(database)
    .WaitFor(keycloak);

builder
    .AddJavaScriptApp("web", "../Web", "start")
    .WithReference(api)
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WaitFor(api);

builder.Build().Run();
