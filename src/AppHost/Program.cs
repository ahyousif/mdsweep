var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("mdsweep");
var dispatcherPassword = builder.AddParameter("dispatcher-password", secret: true);

var api = builder.AddProject<Projects.Mdsweep_Api>("api")
    .WithHttpEndpoint(port: 5080, name: "http")
    .WithReference(database)
    .WithEnvironment("BootstrapDispatcher__Email", "dispatcher@example.test")
    .WithEnvironment("BootstrapDispatcher__Password", dispatcherPassword)
    .WaitFor(database);

builder.AddJavaScriptApp("web", "../Web", "start")
    .WithReference(api)
    .WithHttpEndpoint(port: 4200, targetPort: 4200)
    .WaitFor(api);

builder.Build().Run();
