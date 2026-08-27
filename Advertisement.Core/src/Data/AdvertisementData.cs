using System.Collections.Frozen;
using System.Text.Json;
using Advertisement.Core.Configuration;
using Advertisement.Core.Database;
using Advertisement.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Advertisement.Core.Data;

internal enum AdvertisementSource { Database, Cache, Config }
internal enum AdvertisementOrderMode { Sequential, Random, WeightedRandom }

internal sealed record AdvertisementSettings(
    bool Enabled,
    string DefaultLocale,
    FrozenSet<string> AllowedLocales,
    int IntervalSeconds,
    int RefreshIntervalSeconds,
    int InitialDelaySeconds,
    AdvertisementOrderMode OrderMode,
    bool ExcludeBotsFromPlayers,
    FrozenDictionary<string, string> Colors,
    long ConfigurationVersion);

internal sealed record AdvertisementTag(
    long Id,
    string Key,
    string Color,
    bool Enabled,
    int SortOrder,
    FrozenDictionary<string, string> Translations);

internal sealed record AdvertisementMessage(
    long Id,
    long? ServerId,
    string Key,
    string Name,
    long? TagId,
    string Type,
    bool Enabled,
    int Priority,
    int Weight,
    int SortOrder,
    int? IntervalSeconds,
    int? MinPlayers,
    int? MaxPlayers,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    FrozenDictionary<string, string> Translations)
{
    public bool IsActive(DateTimeOffset now, int playerCount) =>
        Enabled
        && (StartsAt is null || StartsAt <= now)
        && (EndsAt is null || EndsAt >= now)
        && (MinPlayers is null || playerCount >= MinPlayers)
        && (MaxPlayers is null || playerCount <= MaxPlayers);
}

internal sealed record AdvertisementSnapshot(
    AdvertisementSettings Settings,
    FrozenDictionary<long, AdvertisementMessage> Messages,
    FrozenDictionary<long, AdvertisementTag> Tags,
    DateTimeOffset LoadedAt,
    AdvertisementSource Source)
{
    public int ActiveMessageCount(DateTimeOffset now, int playerCount) =>
        Messages.Values.Count(message => message.IsActive(now, playerCount));

    public AdvertisementSnapshot AsCache() => this with { Source = AdvertisementSource.Cache };
}

internal sealed class AdvertisementCache
{
    private AdvertisementSnapshot? _current;
    public AdvertisementSnapshot? Current => Volatile.Read(ref _current);
    public void Replace(AdvertisementSnapshot snapshot) => Interlocked.Exchange(ref _current, snapshot);

    public void MarkDatabaseUnavailable()
    {
        var current = Current;
        if (current?.Source == AdvertisementSource.Database)
        {
            Replace(current.AsCache());
        }
    }
}

internal static class LocaleNormalizer
{
    public static string Normalize(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return string.Empty;
        var value = locale.Trim().Replace('_', '-');
        return value.ToLowerInvariant() switch
        {
            "russian" or "ru-ru" => "ru",
            "english" or "en-us" or "en-gb" => "en",
            "ukrainian" or "uk-ua" => "uk",
            "polish" or "pl-pl" => "pl",
            "german" or "de-de" => "de",
            "pt-br" => "pt-BR",
            "zh-cn" => "zh-CN",
            "zh-tw" => "zh-TW",
            _ when value.Length > 2 && value[2] == '-' => value[..2].ToLowerInvariant(),
            _ => value.ToLowerInvariant(),
        };
    }
}

internal sealed class ConfigAdvertisementProvider(IOptionsMonitor<AdvertisementConfig> options)
{
    public AdvertisementSnapshot Load()
    {
        var config = options.CurrentValue;
        var fallbackLocale = LocaleNormalizer.Normalize(config.DefaultLocale);
        var allowed = config.AllowedLocales.Select(LocaleNormalizer.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Append(fallbackLocale)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        var tags = new Dictionary<long, AdvertisementTag>();
        var tagIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long tagId = -1;
        foreach (var tag in config.Tags.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var id = tagId--;
            tagIds[tag.Key] = id;
            tags[id] = new AdvertisementTag(id, tag.Key, tag.Color, true, tags.Count, Normalize(tag.Translations));
        }

        var messages = new Dictionary<long, AdvertisementMessage>();
        long messageId = -1;
        foreach (var message in config.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var id = messageId--;
            long? resolvedTag = message.Tag is not null && tagIds.TryGetValue(message.Tag, out var value) ? value : null;
            messages[id] = new AdvertisementMessage(
                id, config.ServerId, message.Key,
                string.IsNullOrWhiteSpace(message.Name) ? message.Key : message.Name,
                resolvedTag, message.Type, message.Enabled, message.Priority, Math.Max(0, message.Weight),
                message.SortOrder, message.IntervalSeconds, message.MinPlayers, message.MaxPlayers,
                message.StartsAt, message.EndsAt, Normalize(message.Translations));
        }

        var settings = new AdvertisementSettings(
            config.Enabled, fallbackLocale, allowed, Math.Max(10, config.IntervalSeconds),
            Math.Max(5, config.RefreshIntervalSeconds), Math.Max(0, config.InitialDelaySeconds),
            ParseOrder(config.OrderMode), config.ExcludeBotsFromPlayers,
            config.Colors.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), 0);

        return new AdvertisementSnapshot(settings, messages.ToFrozenDictionary(), tags.ToFrozenDictionary(),
            DateTimeOffset.UtcNow, AdvertisementSource.Config);
    }

    public static AdvertisementOrderMode ParseOrder(string? value) => value?.ToLowerInvariant() switch
    {
        "random" => AdvertisementOrderMode.Random,
        "weighted_random" => AdvertisementOrderMode.WeightedRandom,
        _ => AdvertisementOrderMode.Sequential,
    };

    private static FrozenDictionary<string, string> Normalize(Dictionary<string, string> values) =>
        values.Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(x => LocaleNormalizer.Normalize(x.Key), StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(x => x.Key, x => x.Last().Value, StringComparer.OrdinalIgnoreCase);
}

internal sealed class DatabaseAdvertisementProvider(
    IDbContextFactory<AdvertisementDbContext> contextFactory,
    IOptionsMonitor<AdvertisementConfig> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdvertisementSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var serverId = options.CurrentValue.ServerId;

        var settingsEntity = await context.Settings.AsNoTracking()
            .Where(x => x.ServerId == null || x.ServerId == serverId)
            .OrderByDescending(x => x.ServerId != null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В advertisement.settings отсутствует глобальная настройка.");

        var tags = await context.Tags.AsNoTracking().Include(x => x.Translations)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var messages = await context.Messages.AsNoTracking().Include(x => x.Translations)
            .Where(x => x.ServerId == null || x.ServerId == serverId)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var allowed = JsonSerializer.Deserialize<string[]>(settingsEntity.AllowedLocalesJson, JsonOptions) ?? ["ru"];
        var colors = JsonSerializer.Deserialize<Dictionary<string, string>>(settingsEntity.ColorsJson, JsonOptions) ?? [];
        var settings = new AdvertisementSettings(
            settingsEntity.Enabled, LocaleNormalizer.Normalize(settingsEntity.DefaultLocale),
            allowed.Select(LocaleNormalizer.Normalize).ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            settingsEntity.IntervalSeconds, settingsEntity.RefreshIntervalSeconds, settingsEntity.InitialDelaySeconds,
            ConfigAdvertisementProvider.ParseOrder(settingsEntity.OrderMode), settingsEntity.ExcludeBotsFromPlayers,
            colors.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), settingsEntity.ConfigurationVersion);

        return new AdvertisementSnapshot(
            settings,
            messages.Select(MapMessage).ToFrozenDictionary(x => x.Id),
            tags.Select(MapTag).ToFrozenDictionary(x => x.Id),
            DateTimeOffset.UtcNow,
            AdvertisementSource.Database);
    }

    private static AdvertisementTag MapTag(AdvertisementTagEntity entity) => new(
        entity.Id, entity.Key, entity.Color, entity.Enabled, entity.SortOrder,
        Normalize(entity.Translations.Select(x => (x.Locale, x.Text))));

    private static AdvertisementMessage MapMessage(AdvertisementMessageEntity entity) => new(
        entity.Id, entity.ServerId, entity.Key, entity.Name, entity.TagId, entity.Type, entity.Enabled,
        entity.Priority, entity.Weight, entity.SortOrder, entity.IntervalSeconds, entity.MinPlayers,
        entity.MaxPlayers, entity.StartsAt, entity.EndsAt,
        Normalize(entity.Translations.Select(x => (x.Locale, x.Text))));

    private static FrozenDictionary<string, string> Normalize(IEnumerable<(string Locale, string Text)> values) =>
        values.Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .GroupBy(x => LocaleNormalizer.Normalize(x.Locale), StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(x => x.Key, x => x.Last().Text, StringComparer.OrdinalIgnoreCase);
}

internal sealed class PlayerPreferenceRepository(IDbContextFactory<AdvertisementDbContext> contextFactory)
{
    public async Task<string?> LoadLocaleAsync(ulong steamId, CancellationToken cancellationToken)
    {
        var id = checked((long)steamId);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var locale = await context.PlayerPreferences.AsNoTracking()
            .Where(x => x.SteamId == id).Select(x => x.Locale).SingleOrDefaultAsync(cancellationToken);
        return locale is null ? null : LocaleNormalizer.Normalize(locale);
    }

    public async Task SaveLocaleAsync(ulong steamId, string? locale, CancellationToken cancellationToken)
    {
        var id = checked((long)steamId);
        var normalized = string.IsNullOrWhiteSpace(locale) ? null : LocaleNormalizer.Normalize(locale);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var preference = await context.PlayerPreferences.SingleOrDefaultAsync(x => x.SteamId == id, cancellationToken);
        if (preference is null)
        {
            context.PlayerPreferences.Add(new PlayerPreferenceEntity
            {
                SteamId = id,
                Locale = normalized,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            preference.Locale = normalized;
            preference.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
