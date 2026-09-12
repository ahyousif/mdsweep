using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mdsweep.Utility.Database;

public sealed class ApplicationDatabaseOperations(IApplicationDatabaseManager databaseManager, string connectionString)
{
    public async Task EnsureAndMigrateAsync(CancellationToken cancellationToken = default)
    {
        await databaseManager.EnsureExistsAsync(cancellationToken);
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await databaseManager.DeleteAsync(cancellationToken);
        await EnsureAndMigrateAsync(cancellationToken);
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().ConfigureForMdsweep(connectionString).Options;
        return new ApplicationDbContext(options);
    }
}
