using Npgsql;

namespace Mdsweep.Utility.Database;

public sealed class PostgresApplicationDatabaseManager(string applicationConnectionString) : IApplicationDatabaseManager
{
    private const string DatabaseName = "mdsweep";

    public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetMaintenanceConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'mdsweep'", connection);
        if (await exists.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        await using var create = new NpgsqlCommand("CREATE DATABASE mdsweep", connection);
        await create.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(GetMaintenanceConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var drop = new NpgsqlCommand("DROP DATABASE IF EXISTS mdsweep WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetMaintenanceConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(applicationConnectionString) { Database = "postgres" };
        return builder.ConnectionString;
    }
}
