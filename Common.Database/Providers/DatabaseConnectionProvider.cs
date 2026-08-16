using Npgsql;
using SwiftlyS2.Shared;

namespace Common.Database.Providers;

internal sealed class DatabaseConnectionProvider(ISwiftlyCore core)
{
    private const int DefaultConnectionTimeoutSeconds = 5;

    public string GetPostgreSqlConnectionString(string connectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connection = core.Database.GetConnectionInfo(connectionName);

        if (!string.Equals(connection.Driver, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database connection '{connectionName}' must use PostgreSQL but actual driver: '{connection.Driver}'!"
            );
        }

        if (string.IsNullOrWhiteSpace(connection.Host))
        {
            throw new InvalidOperationException(
                $"Host is not configured for '{connectionName}'."
            );
        }

        if (string.IsNullOrWhiteSpace(connection.Database))
        {
            throw new InvalidOperationException(
                $"Database is not configured for '{connectionName}'."
            );
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port > 0 ? connection.Port : 5432,
            Database = connection.Database,
            Username = connection.User,
            Password = connection.Pass,
            
            Pooling = true,

            Timeout = connection.Timeout > 0 ? checked((int)connection.Timeout) : DefaultConnectionTimeoutSeconds
        };

        return builder.ConnectionString;
    }
}