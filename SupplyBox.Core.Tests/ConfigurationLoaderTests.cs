using SupplyBox.Configuration;
using Xunit;

namespace SupplyBox.Tests;

public sealed class ConfigurationLoaderTests
{
    private static SupplyBoxConfigurationState Initial => new(new(0, new()), "loading");
    private static Task<SupplyBoxSnapshot> FailedDatabase(CancellationToken _) =>
        Task.FromException<SupplyBoxSnapshot>(new IOException("PostgreSQL unavailable"));

    [Fact]
    public async Task HealthyDatabaseDoesNotReadFallback()
    {
        var database = new SupplyBoxSnapshot(7, new());
        var result = await SupplyBoxConfigurationLoader.LoadAsync(_ => Task.FromResult(database),
            _ => throw new InvalidOperationException("Fallback must not be read"), Initial, default);
        Assert.Same(database, result.Snapshot);
        Assert.Equal("database", result.Source);
        Assert.Null(result.DatabaseError);
    }

    [Fact]
    public async Task FailureUsesDownloadedFallbackAndNextExplicitLoadRecovers()
    {
        var document = SupplyBoxDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "fallback.json")));
        document.Settings.DropSoundEvents = ["SupplyBox.Drop.One", "SupplyBox.Drop.Two"];
        document.Maps.Add(new() { Name = "zm_test", Points = [new() { Id = 1, X = -123, Y = 456, Z = 0 }] });
        var previous = new SupplyBoxConfigurationState(new(5, new()), "database");
        var fallback = await SupplyBoxConfigurationLoader.LoadAsync(FailedDatabase,
            _ => Task.FromResult<SupplyBoxDocument?>(document), previous, default);
        Assert.Equal("fallback", fallback.Source);
        Assert.Same(document, fallback.Snapshot.Document);
        Assert.Equal(-123, fallback.Snapshot.Document.Maps[0].Points[0].X);
        Assert.Equal(2, fallback.Snapshot.Document.Settings.DropSoundEvents.Count);
        Assert.IsType<IOException>(fallback.DatabaseError);
        var recovered = await SupplyBoxConfigurationLoader.LoadAsync(_ => Task.FromResult(new SupplyBoxSnapshot(9, new())),
            _ => throw new InvalidOperationException("No fallback when connected"), fallback, default);
        Assert.Equal("database", recovered.Source);
        Assert.Equal(9, recovered.Snapshot.Version);
        Assert.Null(recovered.DatabaseError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrInvalidFallbackKeepsPreviousSnapshot(bool invalid)
    {
        var previous = new SupplyBoxConfigurationState(new(8, new()), "database");
        var result = await SupplyBoxConfigurationLoader.LoadAsync(FailedDatabase,
            _ => invalid ? throw new InvalidDataException("Broken JSON") : Task.FromResult<SupplyBoxDocument?>(null), previous, default);
        Assert.Same(previous.Snapshot, result.Snapshot);
        Assert.Equal("memory", result.Source);
        Assert.Equal(invalid, result.FallbackError is not null);
    }

    [Fact]
    public async Task FirstLoadWithoutDatabaseOrFallbackReportsDefaults()
    {
        var result = await SupplyBoxConfigurationLoader.LoadAsync(FailedDatabase,
            _ => Task.FromResult<SupplyBoxDocument?>(null), Initial, default);
        Assert.Equal("defaults", result.Source);
        Assert.Empty(result.Snapshot.Document.Maps);
    }

    [Fact]
    public async Task InvalidDatabaseDocumentAlsoUsesFallback()
    {
        var document = new SupplyBoxDocument { SchemaVersion = 99 };
        var fallback = new SupplyBoxDocument();
        var result = await SupplyBoxConfigurationLoader.LoadAsync(_ => Task.FromResult(new SupplyBoxSnapshot(1, document)),
            _ => Task.FromResult<SupplyBoxDocument?>(fallback), Initial, default);
        Assert.Equal("fallback", result.Source);
        Assert.Same(fallback, result.Snapshot.Document);
        Assert.IsType<InvalidDataException>(result.DatabaseError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShutdownCancellationNeverPublishesFallback(bool cancelDuringFallback)
    {
        using var shutdown = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupplyBoxConfigurationLoader.LoadAsync(token =>
        {
            if (!cancelDuringFallback) shutdown.Cancel();
            token.ThrowIfCancellationRequested();
            return FailedDatabase(token);
        }, token =>
        {
            Assert.True(cancelDuringFallback);
            shutdown.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult<SupplyBoxDocument?>(new());
        }, Initial, shutdown.Token));
    }
}
