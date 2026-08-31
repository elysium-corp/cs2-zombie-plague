using System.Text.RegularExpressions;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;

namespace Localization.Core.Application;

internal sealed partial class LocalizationRuntime(
    LocalizationCache cache,
    LanguageResolver languageResolver,
    RateLimitedLocalizationLogger logger)
{
    public string? GetForPlayer(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders)
    {
        return GetForLanguage(languageResolver.Resolve(player), key, placeholders);
    }

    public string? GetForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, string>? placeholders)
    {
        var snapshot = cache.Current;
        if (snapshot is null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalizedKey = key.Trim();
        if (!snapshot.Entries.TryGetValue(normalizedKey, out var entry))
        {
            LogMissing(snapshot, normalizedKey, "key");
            return null;
        }

        var requested = LocaleNormalizer.Normalize(languageCode);
        if (!snapshot.IsLanguageEnabled(requested))
        {
            requested = snapshot.Settings.ServerFallbackLanguage;
        }

        if (!entry.Translations.TryGetValue(requested, out var text)
            && !entry.Translations.TryGetValue(snapshot.Settings.ServerFallbackLanguage, out text))
        {
            LogMissing(snapshot, normalizedKey, $"translation:{requested}");
            return null;
        }

        return placeholders is null || placeholders.Count == 0
            ? text
            : PlaceholderRegex().Replace(text, match =>
                placeholders.TryGetValue(match.Groups["name"].Value, out var value)
                    ? value
                    : match.Value);
    }

    private void LogMissing(LocalizationSnapshot snapshot, string key, string kind)
    {
        if (!snapshot.Settings.LogMissingKeys)
        {
            return;
        }

        logger.Warning(
            $"missing:{kind}:{key}",
            TimeSpan.FromMinutes(5),
            "[Localization] Отсутствует {Kind} для ключа {Key}.",
            kind,
            key);
    }

    [GeneratedRegex(@"\{(?<name>[a-z][a-z0-9_]*)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}

internal sealed class PlayerLanguageSelectionService(
    LocalizationCache cache,
    PlayerLanguageCache playerLanguageCache,
    PlayerLanguagePreferenceRepository repository)
{
    public async Task SetAsync(
        ulong steamId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var normalized = LocaleNormalizer.Normalize(languageCode);
        var snapshot = cache.Current
            ?? throw new InvalidOperationException("Snapshot локализации ещё не загружен.");
        if (!snapshot.IsLanguageEnabled(normalized))
        {
            throw new InvalidOperationException($"Язык '{normalized}' отсутствует или отключён.");
        }

        await repository.SaveAsync(steamId, normalized, cancellationToken);
        playerLanguageCache.SetManual(steamId, normalized);
    }
}

internal sealed class RateLimitedLocalizationLogger(ILogger logger)
{
    private const int MaximumKeys = 1024;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _last = new(StringComparer.Ordinal);

    public void Warning(string key, TimeSpan interval, string message, params object?[] args)
    {
        var now = DateTimeOffset.UtcNow;
        if (_last.Count >= MaximumKeys && !_last.ContainsKey(key))
        {
            foreach (var stale in _last.Where(item => now - item.Value >= interval).Take(128))
            {
                _last.TryRemove(stale.Key, out _);
            }

            if (_last.Count >= MaximumKeys)
            {
                key = "rate-limit:overflow";
            }
        }

        var previous = _last.GetOrAdd(key, DateTimeOffset.MinValue);
        if (now - previous < interval || !_last.TryUpdate(key, now, previous))
        {
            return;
        }

        logger.LogWarning(message, args);
    }
}
