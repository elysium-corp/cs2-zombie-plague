using Npgsql;
using SwiftlyS2.Shared;

namespace Economy.Core.Database;

internal sealed class DatabaseConnectionProvider(ISwiftlyCore core)
{
    public string GetPostgreSqlConnectionString(string connectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connection = core.Database.GetConnectionInfo(connectionName);

        if (!string.Equals(connection.Driver, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database connection '{connectionName}' must use PostgreSQL. " + 
                $"Actual driver: '{connection.Driver}'.");
        }

        if (string.IsNullOrWhiteSpace(connection.Host))
        {
            throw new InvalidOperationException($"Host is not configured for '{connectionName}'.");
        }

        if (string.IsNullOrWhiteSpace(connection.Database))
        {
            throw new InvalidOperationException($"Database is not configured for '{connectionName}'.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port > 0 ? connection.Port : 5432,
            Database = connection.Database,
            Username = connection.User,
            Password = connection.Pass,
            Pooling = true
        };

        if (connection.Timeout > 0)
        {
            builder.Timeout = checked((int)connection.Timeout);
        }

        return builder.ConnectionString;
    }
}