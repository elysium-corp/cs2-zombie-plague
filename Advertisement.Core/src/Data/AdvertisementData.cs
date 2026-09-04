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
internal enum AdvertisementDispatchMode { Periodic, Daily, Manual }
internal enum AdvertisementAudienceType { All, AdminGroup }

internal sealed record AdvertisementSettings(
    bool Enabled,
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
    FrozenDictionary<string, string> Translations)
{
    public string? ResolveText(string? languageCode, string? fallbackLanguageCode)
    {
        var requested = languageCode?.Trim();
        if (!string.IsNullOrWhiteSpace(requested)
            && Translations.TryGetValue(requested, out var requestedText)
            && !string.IsNullOrWhiteSpace(requestedText))
        {
            return requestedText;
        }

        var fallback = fallbackLanguageCode?.Trim();
        return !string.IsNullOrWhiteSpace(fallback)
               && Translations.TryGetValue(fallback, out var fallbackText)
               && !string.IsNullOrWhiteSpace(fallbackText)
            ? fallbackText
            : null;
    }
}

internal sealed record AdvertisementMessage(
    long Id,
    string Key,
    string Name,
    string LocalizationKey,
    long? TagId,
    string Type,
    bool Enabled,
    int Priority,
    int Weight,
    int SortOrder,
    int? IntervalSeconds,
    AdvertisementDispatchMode DispatchMode,
    FrozenSet<TimeOnly> DailyTimes,
    TimeOnly? DailyStartTime,
    TimeOnly? DailyEndTime,
    AdvertisementAudienceType AudienceType,
    string? AudienceGroup,
    int? MinPlayers,
    int? MaxPlayers,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt)
{
    public bool IsActive(DateTimeOffset now, int playerCount)
    {
        var localTime = TimeOnly.FromDateTime(now.LocalDateTime);

        return Enabled
               && (StartsAt is null || StartsAt <= now)
               && (EndsAt is null || EndsAt >= now)
               && (MinPlayers is null || playerCount >= MinPlayers)
               && (MaxPlayers is null || playerCount <= MaxPlayers)
               && IsInsideDailyWindow(localTime);
    }

    private bool IsInsideDailyWindow(TimeOnly time)
    {
        var start = DailyStartTime;
        var end = DailyEndTime;

        if (start is null && end is null)
        {
            return true;
        }

        if (start is null)
        {
            return time <= end!.Value;
        }

        if (end is null)
        {
            return time >= start.Value;
        }

        return start.Value <= end.Value
            ? time >= start.Value && time <= end.Value
            : time >= start.Value || time <= end.Value;
    }
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

internal static class DeliveryRuleParser
{
    public static AdvertisementDispatchMode ParseDispatchMode(string? value) => value?.ToLowerInvariant() switch
    {
        "daily" => AdvertisementDispatchMode.Daily,
        "manual" => AdvertisementDispatchMode.Manual,
        _ => AdvertisementDispatchMode.Periodic,
    };

    public static AdvertisementAudienceType ParseAudienceType(string? value) =>
        string.Equals(value, "admin_group", StringComparison.OrdinalIgnoreCase)
            ? AdvertisementAudienceType.AdminGroup
            : AdvertisementAudienceType.All;

    public static TimeOnly? ParseTime(string? value)
    {
        return TimeOnly.TryParse(value, out var time) ? time : null;
    }

    public static FrozenSet<TimeOnly> ParseDailyTimes(IEnumerable<string>? values)
    {
        return (values ?? [])
            .Select(ParseTime)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToFrozenSet();
    }

    public static FrozenSet<TimeOnly> ParseDailyTimesJson(string json)
    {
        var values = JsonSerializer.Deserialize<string[]>(json) ?? [];
        return ParseDailyTimes(values);
    }
}

internal sealed class ConfigAdvertisementProvider(IOptionsMonitor<AdvertisementConfig> options)
{
    public AdvertisementSnapshot Load()
    {
        var config = options.CurrentValue;

        var tags = new Dictionary<long, AdvertisementTag>();
        var tagIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long tagId = -1;
        foreach (var tag in config.Tags.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var id = tagId--;
            tagIds[tag.Key] = id;
            var translations = NormalizeTagTranslations(tag.Translations);
            tags[id] = new AdvertisementTag(
                id,
                tag.Key,
                tag.Color,
                true,
                tags.Count,
                translations);
        }

        var messages = new Dictionary<long, AdvertisementMessage>();
        long messageId = -1;
        foreach (var message in config.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var id = messageId--;
            long? resolvedTag = message.Tag is not null && tagIds.TryGetValue(message.Tag, out var value) ? value : null;
            messages[id] = new AdvertisementMessage(
                id, message.Key,
                string.IsNullOrWhiteSpace(message.Name) ? message.Key : message.Name,
                string.IsNullOrWhiteSpace(message.LocalizationKey)
                    ? $"advertisement.messages.{message.Key}"
                    : message.LocalizationKey.Trim(),
                resolvedTag, message.Type, message.Enabled, message.Priority, Math.Max(0, message.Weight),
                message.SortOrder, message.IntervalSeconds,
                DeliveryRuleParser.ParseDispatchMode(message.DispatchMode),
                DeliveryRuleParser.ParseDailyTimes(message.DailyTimes),
                DeliveryRuleParser.ParseTime(message.DailyStartTime),
                DeliveryRuleParser.ParseTime(message.DailyEndTime),
                DeliveryRuleParser.ParseAudienceType(message.AudienceType),
                string.IsNullOrWhiteSpace(message.AudienceGroup) ? null : message.AudienceGroup.Trim(),
                message.MinPlayers, message.MaxPlayers,
                message.StartsAt, message.EndsAt);
        }

        var settings = new AdvertisementSettings(
            config.Enabled, Math.Max(10, config.IntervalSeconds),
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

    private static FrozenDictionary<string, string> NormalizeTagTranslations(
        IEnumerable<KeyValuePair<string, string>> translations) => translations
        .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
        .GroupBy(item => item.Key.Trim(), StringComparer.OrdinalIgnoreCase)
        .ToFrozenDictionary(
            group => group.Key,
            group => group.Last().Value.Trim(),
            StringComparer.OrdinalIgnoreCase);

}

internal sealed class DatabaseAdvertisementProvider(
    IDbContextFactory<AdvertisementDbContext> contextFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdvertisementSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settingsEntity = await context.Settings.AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В advertisement.settings отсутствует настройка.");

        var tags = await context.Tags.AsNoTracking()
            .Include(tag => tag.Translations)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var messages = await context.Messages.AsNoTracking()
            .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var colors = JsonSerializer.Deserialize<Dictionary<string, string>>(settingsEntity.ColorsJson, JsonOptions) ?? [];
        var settings = new AdvertisementSettings(
            settingsEntity.Enabled, settingsEntity.IntervalSeconds,
            settingsEntity.RefreshIntervalSeconds, settingsEntity.InitialDelaySeconds,
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
        entity.Id,
        entity.Key,
        entity.Color,
        entity.Enabled,
        entity.SortOrder,
        entity.Translations
            .Where(translation => !string.IsNullOrWhiteSpace(translation.Locale)
                                  && !string.IsNullOrWhiteSpace(translation.Text))
            .GroupBy(translation => translation.Locale.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                group => group.Key,
                group => group.Last().Text.Trim(),
                StringComparer.OrdinalIgnoreCase));

    private static AdvertisementMessage MapMessage(AdvertisementMessageEntity entity) => new(
        entity.Id, entity.Key, entity.Name, entity.LocalizationKey, entity.TagId, entity.Type, entity.Enabled,
        entity.Priority, entity.Weight, entity.SortOrder, entity.IntervalSeconds,
        DeliveryRuleParser.ParseDispatchMode(entity.DispatchMode),
        DeliveryRuleParser.ParseDailyTimesJson(entity.DailyTimesJson),
        entity.DailyStartTime, entity.DailyEndTime,
        DeliveryRuleParser.ParseAudienceType(entity.AudienceType),
        string.IsNullOrWhiteSpace(entity.AudienceGroup) ? null : entity.AudienceGroup.Trim(),
        entity.MinPlayers, entity.MaxPlayers, entity.StartsAt, entity.EndsAt);
}
