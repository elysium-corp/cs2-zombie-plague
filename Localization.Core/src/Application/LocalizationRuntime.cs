using System.Globalization;
using System.Text.RegularExpressions;
using Localization.Api;
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
        Dictionary<string, object?>? values = null;
        if (placeholders is not null)
        {
            values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in placeholders)
            {
                values[name] = value;
            }
        }

        return FormatForLanguage(languageCode, key, values, validateSchema: false);
    }

    public string? FormatForPlayer(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, object?> parameters)
    {
        return FormatForLanguage(languageResolver.Resolve(player), key, parameters);
    }

    public string? FormatForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        return FormatForLanguage(languageCode, key, parameters, validateSchema: true);
    }

    private string? FormatForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, object?>? parameters,
        bool validateSchema)
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

        if (!validateSchema)
        {
            var renderedText = LocalizationMarkupRenderer.Render(text, snapshot.Settings.ColorTags);
            var legacyResult = parameters is null || parameters.Count == 0
                ? renderedText
                : PlaceholderRegex().Replace(renderedText, match =>
                    TryGetValue(parameters, match.Groups["name"].Value, out var value)
                        ? SanitizeParameterValue(
                            Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                        : match.Value);
            return legacyResult;
        }

        if (entry.Parameters.Count == 0)
        {
            return LocalizationMarkupRenderer.Render(text, snapshot.Settings.ColorTags);
        }

        var formatted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in entry.Parameters.Values)
        {
            if (!TryGetValue(parameters, definition.Name, out var value) || value is null)
            {
                if (definition.IsRequired)
                {
                    LogInvalidParameter(snapshot, normalizedKey, definition.Name, "required");
                    return null;
                }

                formatted[definition.Name] = string.Empty;
                continue;
            }

            if (!LocalizationParameterSchema.TryFormatValue(definition.Type, value, out var result))
            {
                LogInvalidParameter(snapshot, normalizedKey, definition.Name, "type");
                return null;
            }

            formatted[definition.Name] = SanitizeParameterValue(result);
        }

        var rendered = LocalizationMarkupRenderer.Render(text, snapshot.Settings.ColorTags);
        return PlaceholderRegex().Replace(rendered, match =>
            formatted.TryGetValue(match.Groups["name"].Value, out var value)
                ? value
                : match.Value);
    }

    public IReadOnlyList<LocalizationParameterDefinition> GetParameterDefinitions(string key)
    {
        var snapshot = cache.Current;
        if (string.IsNullOrWhiteSpace(key)
            || snapshot is null
            || !snapshot.Entries.TryGetValue(key.Trim(), out var entry))
        {
            return [];
        }

        return entry.Parameters.Values
            .OrderBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public LocalizationTag? GetTagForPlayer(IPlayer player, string tagKey)
    {
        return GetTagForLanguage(languageResolver.Resolve(player), tagKey);
    }

    public LocalizationTag? GetTagForLanguage(string languageCode, string tagKey)
    {
        var snapshot = cache.Current;
        if (snapshot is null || string.IsNullOrWhiteSpace(tagKey))
        {
            return null;
        }

        var normalizedKey = tagKey.Trim();
        if (!snapshot.Tags.TryGetValue(normalizedKey, out var tag) || !tag.Enabled)
        {
            return null;
        }

        var text = GetForLanguage(languageCode, tag.LocalizationKey, null);
        return string.IsNullOrWhiteSpace(text)
            ? null
            : new LocalizationTag(tag.Key, text, tag.Color);
    }

    private static bool TryGetValue(
        IReadOnlyDictionary<string, object?>? parameters,
        string name,
        out object? value)
    {
        if (parameters is not null)
        {
            if (parameters.TryGetValue(name, out value))
            {
                return true;
            }

            foreach (var parameter in parameters)
            {
                if (string.Equals(parameter.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = parameter.Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static string SanitizeParameterValue(string value)
    {
        return SemanticMarkupRegex().Replace(
            RawColorRegex().Replace(value, string.Empty),
            string.Empty);
    }

    private void LogInvalidParameter(
        LocalizationSnapshot snapshot,
        string key,
        string parameter,
        string reason)
    {
        if (!snapshot.Settings.LogMissingKeys)
        {
            return;
        }

        logger.Warning(
            $"invalid-parameter:{reason}:{key}:{parameter}",
            TimeSpan.FromMinutes(5),
            "[Localization] Параметр {Parameter} ключа {Key} не прошёл проверку {Reason}.",
            parameter,
            key,
            reason);
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

    [GeneratedRegex(
        @"\[(?:/?|default|white|darkred|lightpurple|green|olive|lime|red|gr[ae]y|lightyellow|yellow|silver|bluegrey|lightblue|blue|darkblue|purple|magenta|lightred|gold|orange)\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RawColorRegex();

    [GeneratedRegex(
        @"\{/?[a-z][a-z0-9_]*(?::[a-z]+)?\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SemanticMarkupRegex();
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
