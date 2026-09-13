using Mdsweep.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("azure");

var postgres = builder.AddPostgres();

var database = postgres.AddDatabase("mdsweep");

var keycloakDatabase = postgres.AddDatabase("keycloak-db", databaseName: "keycloak");

var keycloak = builder.AddKeycloak(postgres, keycloakDatabase);

var mailpit = builder.ExecutionContext.IsRunMode ? builder.AddMailpit() : null;
var communications = builder.AddCommunications(mailpit);

builder.AddMdsweepUtility(postgres, database, communications);

var api = builder.AddApi(database, keycloak, communications);

var web = builder.AddWeb(api);

api.PublishWithContainerFiles(web, "./wwwroot");

await builder.Build().RunAsync();
