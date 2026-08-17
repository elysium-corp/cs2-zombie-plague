namespace Common.Database;

public sealed record DatabaseOptions
{
    public const string DefaultMigrationsHistoryTable = "__ef_migrations_history";
    
    public required string ConnectionName { get; init; }

    public required string Schema { get; init; }

    public int CommandTimeoutSeconds { get; init; } = 5;

    public int RetryCount { get; init; } = 2;

    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(3);

    public string MigrationsHistoryTable { get; init; } = DefaultMigrationsHistoryTable;

    public bool IgnoreExecutedCommandLogs { get; init; } = true;
}