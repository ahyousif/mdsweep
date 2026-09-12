using System.Diagnostics;
using Azure.Identity;
using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Configuration;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Invitations.DomainEventHandlers;
using Mdsweep.Infrastructure;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Utility.Commands;
using Mdsweep.Utility.Database;
using Mdsweep.Utility.TenantProvisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Wolverine;

if (!UtilityArguments.TryParse(args, out var command, out var error))
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(UtilityArguments.Usage);
    return 2;
}

var builder = Host.CreateApplicationBuilder();
var connectionString = builder.Configuration.GetConnectionString("mdsweep");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("The MDSweep database connection 'ConnectionStrings:mdsweep' is not configured.");
    return 1;
}

builder.UseWolverine(options => options.AddPersistence(builder.Configuration));
builder.Services.AddScoped(_ => new ApplicationDbContext(
    new DbContextOptionsBuilder<ApplicationDbContext>().ConfigureForMdsweep(connectionString).Options
));
builder.Services.AddScoped<IRepository>(services => services.GetRequiredService<ApplicationDbContext>());
builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton<ITokenService, SecureTokenService>();
builder.Services.AddMdsweepEmail(builder.Configuration);
builder
    .Services.AddOptions<WebOptions>()
    .Bind(builder.Configuration.GetSection(WebOptions.SectionName))
    .Validate(
        options =>
            Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Web base URL must be an absolute HTTP or HTTPS URL."
    )
    .ValidateOnStart();
builder.Services.AddScoped<SendEmailWhenInvitationCreatedHandler>();
builder.Services.AddScoped<TenantProvisioningService>();

using var host = builder.Build();
using var httpClient = new HttpClient();
var databaseManager = CreateDatabaseManager(builder.Configuration, connectionString, httpClient);
var databaseOperations = new ApplicationDatabaseOperations(databaseManager, connectionString);

try
{
    switch (command)
    {
        case UtilityCommand.DatabaseMigrate:
            await databaseOperations.EnsureAndMigrateAsync();
            Console.WriteLine("The mdsweep database exists and all checked-in migrations are applied.");
            return 0;

        case UtilityCommand.DatabaseReset:
            await databaseOperations.ResetAsync();
            Console.WriteLine("The mdsweep database was reset and migrated. The keycloak database was not modified.");
            return 0;

        case UtilityCommand.TenantProvision provision:
            await databaseOperations.EnsureAndMigrateAsync();
            await JasperFx.Resources.ResourceHostExtensions.SetupResources(host);
            await using (var scope = host.Services.CreateAsyncScope())
            {
                var result = await scope
                    .ServiceProvider.GetRequiredService<TenantProvisioningService>()
                    .ExecuteAsync(provision.Options);
                var writer = result.Succeeded ? Console.Out : Console.Error;
                writer.WriteLine(result.Message);
                return result.Succeeded ? 0 : 1;
            }

        default:
            throw new UnreachableException();
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Utility operation failed: {exception.Message}");
    Console.Error.WriteLine("No credentials or invitation tokens were printed.");
    return 1;
}

static IApplicationDatabaseManager CreateDatabaseManager(
    IConfiguration configuration,
    string applicationConnectionString,
    HttpClient httpClient
)
{
    var subscriptionId = configuration["Azure:SubscriptionId"];
    var resourceGroup = configuration["Azure:ResourceGroup"];
    var serverName = configuration["Azure:PostgresServerName"];

    if (
        !string.IsNullOrWhiteSpace(subscriptionId)
        && !string.IsNullOrWhiteSpace(resourceGroup)
        && !string.IsNullOrWhiteSpace(serverName)
    )
    {
        return new AzurePostgresApplicationDatabaseManager(
            httpClient,
            new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()),
            subscriptionId,
            resourceGroup,
            serverName
        );
    }

    return new PostgresApplicationDatabaseManager(applicationConnectionString);
}
