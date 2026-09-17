using Mdsweep.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("azure");

var postgres = builder.AddPostgres();

var database = postgres.AddDatabase("mdsweep");

var keycloakDatabase = postgres.AddDatabase("keycloak-db", databaseName: "keycloak");

var keycloak = builder.AddKeycloak(postgres, keycloakDatabase);

var oidcClientSecret = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("oidc-client-secret", "Development-only-secret", secret: true)
    : builder.AddParameter("oidc-client-secret", secret: true);
var keycloakAutomationClientSecret = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("keycloak-automation-client-secret", "Development-only-automation-secret", secret: true)
    : builder.AddParameter("keycloak-automation-client-secret", secret: true);
var demoAdminPassword = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("demo-admin-password", "Development-only-demo-password", secret: true)
    : builder.AddParameter("demo-admin-password", secret: true);

var mailpit = builder.ExecutionContext.IsRunMode ? builder.AddMailpit() : null;
var communications = builder.AddCommunications(mailpit);

builder.AddUtility(
    postgres,
    database,
    keycloak,
    communications,
    oidcClientSecret,
    keycloakAutomationClientSecret,
    demoAdminPassword
);

var api = builder.AddApi(database, keycloak, communications, oidcClientSecret);

var web = builder.AddWeb(api);

api.PublishWithContainerFiles(web, "./wwwroot");

await builder.Build().RunAsync();
