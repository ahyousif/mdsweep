using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Utility.BootstrapTenant;
using Microsoft.EntityFrameworkCore;

if (!BootstrapTenantArguments.TryParse(args, out var options, out var error))
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(BootstrapTenantArguments.Usage);
    return 2;
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__mdsweep");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Bootstrap aborted.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("The MDSweep database connection 'ConnectionStrings:mdsweep' is not configured.");
    return 1;
}

var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().ConfigureForMdsweep(connectionString).Options;
await using var db = new ApplicationDbContext(dbOptions);

Console.WriteLine($"Bootstrapping tenant {options!.TenantId}...");
Console.WriteLine();

try
{
    var result = await new BootstrapTenantService(db).ExecuteAsync(options);
    var writer = result.Succeeded ? Console.Out : Console.Error;
    writer.WriteLine(result.Message);
    return result.Succeeded ? 0 : 1;
}
catch (Exception)
{
    Console.Error.WriteLine("Bootstrap aborted.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("An unexpected error occurred. No database credentials were printed; inspect the job logs.");
    return 1;
}
