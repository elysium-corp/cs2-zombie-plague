using Localization.Core.Application;
using Localization.Core.Configuration;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Localization.Core.Tests;

public sealed class LocalizationPollingTests
{
    [Fact]
    public async Task PollingUsesConfiguredIntervalAndRetainsLiveTranslationsWhenDatabaseFails()
    {
        var cache = new LocalizationCache();
        var config = System.Text.Json.JsonSerializer.Deserialize<LocalizationFallbackConfig>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.jsonc")),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        config.RefreshIntervalSeconds = 5;
        var live = FallbackLocalizationProvider.Build(config, LocalizationSource.Database);
        cache.Replace(live);
        var factory = new OfflineFactory();
        var options = new UnavailableFallback();
        using var coordinator = new LocalizationCoordinator(cache, new DatabaseLocalizationProvider(factory),
            new FallbackLocalizationProvider(options), new RateLimitedLocalizationLogger(NullLogger.Instance));
        coordinator.Start();
        coordinator.Start();
        await factory.Polled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        coordinator.Dispose();
        Assert.Equal(2, factory.Reads);
        Assert.Equal(1, options.Reads);
        Assert.Same(live, cache.Current);
        coordinator.Start();
        Assert.Equal(2, factory.Reads);
    }

    private sealed class OfflineFactory : IDbContextFactory<LocalizationDbContext>
    {
        public int Reads;
        public TaskCompletionSource Polled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public LocalizationDbContext CreateDbContext() => throw new InvalidOperationException("offline");
        public Task<LocalizationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref Reads) == 2) Polled.TrySetResult();
            return Task.FromException<LocalizationDbContext>(new IOException("offline"));
        }
    }

    private sealed class UnavailableFallback : IOptionsMonitor<LocalizationFallbackConfig>
    {
        public int Reads;
        public LocalizationFallbackConfig CurrentValue
        {
            get { Interlocked.Increment(ref Reads); throw new IOException("fallback unavailable"); }
        }
        public LocalizationFallbackConfig Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<LocalizationFallbackConfig, string?> listener) => null;
    }
}
