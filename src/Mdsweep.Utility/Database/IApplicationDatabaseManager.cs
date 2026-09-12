namespace Mdsweep.Utility.Database;

public interface IApplicationDatabaseManager
{
    Task EnsureExistsAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
