using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;
using ZombiePlague.Core.Catalog;

namespace ZombiePlague.Core.Tests;

public sealed class ZombieCatalogDiagnosticsTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.UndefinedTable)]
    [InlineData(PostgresErrorCodes.InvalidSchemaName)]
    public void MissingCatalogReportsWebInitializationWithoutAnOutageStackTrace(string sqlState)
    {
        var error = new PostgresException("Локализованное сообщение PostgreSQL", "ERROR", "ERROR", sqlState);
        var logger = new RecordingLogger();

        ZombieCatalogDiagnostics.LogDatabaseFailure(logger, error, "fallback");

        Assert.True(ZombieCatalogDiagnostics.NeedsInitialization(error));
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("Каталог в БД не подготовлен", entry.Message);
        Assert.Contains("источник fallback", entry.Message);
        Assert.Contains("zombie_catalog.json", entry.Message);
        Assert.Contains("Elysium → Классы", entry.Message);
        Assert.Contains("zp_classes_reload", entry.Message);
        Assert.DoesNotContain("БД недоступна", entry.Message);
    }

    [Fact]
    public async Task EmptyCatalogKeepsFallbackAndProvidesTheSameSetupInstructions()
    {
        var document = ZombieCatalogDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));
        var error = new ZombieCatalogNotInitializedException();
        var state = await ZombieCatalogLoader.LoadAsync(_ => throw error,
            _ => Task.FromResult(document), null, CancellationToken.None);
        var logger = new RecordingLogger();

        ZombieCatalogDiagnostics.LogDatabaseFailure(logger, state.DatabaseError!, state.Source);

        Assert.Equal("fallback", state.Source);
        Assert.Same(document, state.Document);
        Assert.Same(error, state.DatabaseError);
        Assert.True(ZombieCatalogDiagnostics.NeedsInitialization(state.DatabaseError));
        var entry = Assert.Single(logger.Entries);
        Assert.Null(entry.Exception);
        Assert.Contains(ZombieCatalogDiagnostics.InitializationHelp, entry.Message);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ConnectionFailure)]
    [InlineData(PostgresErrorCodes.InsufficientPrivilege)]
    [InlineData(PostgresErrorCodes.UndefinedColumn)]
    public void OtherDatabaseFailuresKeepTheActualCauseAndDoNotSuggestInitialization(string sqlState)
    {
        var error = new PostgresException("Ошибка подключения, прав или совместимости", "ERROR", "ERROR", sqlState);
        var logger = new RecordingLogger();

        ZombieCatalogDiagnostics.LogDatabaseFailure(logger, error, "memory");

        Assert.False(ZombieCatalogDiagnostics.NeedsInitialization(error));
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(error, entry.Exception);
        Assert.Contains("источник memory", entry.Message);
        Assert.DoesNotContain(ZombieCatalogDiagnostics.InitializationHelp, entry.Message);
    }

    [Fact]
    public void NetworkFailuresKeepTheirExceptionAndSuccessfulLoadsNeedNoInitialization()
    {
        var error = new IOException("Connection reset");
        var logger = new RecordingLogger();

        ZombieCatalogDiagnostics.LogDatabaseFailure(logger, error, "fallback");

        Assert.False(ZombieCatalogDiagnostics.NeedsInitialization(error));
        Assert.False(ZombieCatalogDiagnostics.NeedsInitialization(null));
        Assert.Same(error, Assert.Single(logger.Entries).Exception);
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add(new(logLevel, formatter(state, exception), exception));
    }
}
