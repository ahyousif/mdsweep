using Mdsweep.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("azure");

var postgres = builder.AddMdsweepPostgres();

var database = postgres.AddDatabase("mdsweep");

var keycloakDatabase = postgres.AddDatabase("keycloak-db", databaseName: "keycloak");

var keycloak = builder.AddMdsweepKeycloak(postgres, keycloakDatabase);

var mailpit = builder.ExecutionContext.IsRunMode ? builder.AddMdsweepMailpit() : null;
var communications = builder.AddMdsweepCommunications(mailpit);

builder.AddMdsweepUtility(postgres, database, communications);

var api = builder.AddMdsweepApi(database, keycloak, communications);

var web = builder.AddMdsweepWeb(api);

api.PublishWithContainerFiles(web, "./wwwroot");

await builder.Build().RunAsync();
