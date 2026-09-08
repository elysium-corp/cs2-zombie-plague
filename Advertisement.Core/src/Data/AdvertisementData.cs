using Advertisement.Core.Application;
using CustomHud.Api;
using System.Collections.Frozen;
using System.Text.Json;
using Advertisement.Core.Configuration;
using Advertisement.Core.Database;
using Advertisement.Core.Database.Entities;
using Localization.Api;
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
    long ConfigurationVersion);

internal sealed record AdvertisementMessage(
    long Id,
    string Key,
    string Name,
    string? LocalizationKey,
    string? TagKey,
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
    public AdvertisementPresentation? Presentation { get; init; }

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
    DateTimeOffset LoadedAt,
    AdvertisementSource Source)
{
    public FrozenDictionary<string, BannerNotificationRule> Notifications { get; init; } = NotificationCatalog.Defaults;
    public FrozenDictionary<string, HudWidgetOptions> Widgets { get; init; } = new Dictionary<string, HudWidgetOptions>
        { ["ZombiePlague.Abilities"] = new() }.ToFrozenDictionary();

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

        var messages = new Dictionary<long, AdvertisementMessage>();
        long messageId = -1;
        foreach (var message in config.Messages.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
        {
            var id = messageId--;
            var key = LocalizationKey.Canonicalize(message.Key);
            messages[id] = new AdvertisementMessage(
                id, key,
                string.IsNullOrWhiteSpace(message.Name) ? key : message.Name,
                message.DisplayType == "hud" ? null : string.IsNullOrWhiteSpace(message.LocalizationKey)
                    ? $"Advertisement.Messages.{key}"
                    : LocalizationKey.Canonicalize(message.LocalizationKey),
                NormalizeTagKey(message.Tag), message.Type, message.Enabled, message.Priority, Math.Max(0, message.Weight),
                message.SortOrder, message.IntervalSeconds,
                DeliveryRuleParser.ParseDispatchMode(message.DispatchMode),
                DeliveryRuleParser.ParseDailyTimes(message.DailyTimes),
                DeliveryRuleParser.ParseTime(message.DailyStartTime),
                DeliveryRuleParser.ParseTime(message.DailyEndTime),
                DeliveryRuleParser.ParseAudienceType(message.AudienceType),
                string.IsNullOrWhiteSpace(message.AudienceGroup) ? null : message.AudienceGroup.Trim(),
                message.MinPlayers, message.MaxPlayers,
                message.StartsAt, message.EndsAt)
            {
                Presentation = message.DisplayType is null ? null : new(
                    message.DisplayType,
                    string.IsNullOrWhiteSpace(message.HudLocalizationKey) ? null : LocalizationKey.Canonicalize(message.HudLocalizationKey),
                    message.HudPosition, message.HudDurationSeconds, message.HudStyle)
                {
                    Template = message.BannerTemplate, HeaderKey = message.BannerHeaderKey, TitleKey = message.BannerTitleKey,
                    Parameters = message.BannerParameters.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
                }
            };
        }

        var settings = new AdvertisementSettings(
            config.Enabled, Math.Max(10, config.IntervalSeconds),
            Math.Max(5, config.RefreshIntervalSeconds), Math.Max(0, config.InitialDelaySeconds),
            ParseOrder(config.OrderMode), config.ExcludeBotsFromPlayers, 0);

        return new AdvertisementSnapshot(settings, messages.ToFrozenDictionary(),
            DateTimeOffset.UtcNow, AdvertisementSource.Config)
        {
            Notifications = (config.Notifications ?? NotificationCatalog.Defaults.Values.ToList())
                .Select(rule => { NotificationCatalog.Validate(rule); return rule; }).ToFrozenDictionary(rule => rule.EventKey),
            Widgets = (config.Widgets ?? new() { ["ZombiePlague.Abilities"] = new() }).ToFrozenDictionary()
        };
    }

    public static AdvertisementOrderMode ParseOrder(string? value) => value?.ToLowerInvariant() switch
    {
        "random" => AdvertisementOrderMode.Random,
        "weighted_random" => AdvertisementOrderMode.WeightedRandom,
        _ => AdvertisementOrderMode.Sequential,
    };

    private static string? NormalizeTagKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : LocalizationKey.Canonicalize(value);
}

internal sealed class DatabaseAdvertisementProvider(IDbContextFactory<AdvertisementDbContext> contextFactory)
{
    internal sealed record RuntimeSettings(
        bool Enabled,
        int IntervalSeconds,
        int RefreshIntervalSeconds,
        int InitialDelaySeconds,
        string OrderMode,
        bool ExcludeBotsFromPlayers,
        long ConfigurationVersion);

    public async Task<AdvertisementSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settingsEntity = await BuildRuntimeSettingsQuery(context)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В advertisement.settings отсутствует настройка.");

        var messages = await context.Messages.AsNoTracking().Include(x => x.BannerTemplate)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var settings = new AdvertisementSettings(
            settingsEntity.Enabled, settingsEntity.IntervalSeconds,
            settingsEntity.RefreshIntervalSeconds, settingsEntity.InitialDelaySeconds,
            ConfigAdvertisementProvider.ParseOrder(settingsEntity.OrderMode),
            settingsEntity.ExcludeBotsFromPlayers,
            settingsEntity.ConfigurationVersion);

        return new AdvertisementSnapshot(
            settings,
            messages.Select(MapMessage).ToFrozenDictionary(x => x.Id),
            DateTimeOffset.UtcNow,
            AdvertisementSource.Database)
        {
            Notifications = (await context.Set<NotificationRuleEntity>().AsNoTracking().Include(x => x.Template).ToListAsync(cancellationToken))
                .Select(MapNotification).ToFrozenDictionary(rule => rule.EventKey),
            Widgets = (await context.Set<HudWidgetEntity>().AsNoTracking().ToListAsync(cancellationToken))
                .ToFrozenDictionary(item => item.Key, item => JsonSerializer.Deserialize<HudWidgetOptions>(item.SettingsJson) ?? new())
        };
    }

    internal static BannerNotificationRule MapNotification(NotificationRuleEntity row)
    {
        var rule = (JsonSerializer.Deserialize<BannerNotificationRule>(row.SettingsJson) ?? new()) with
        {
            EventKey = row.EventKey,
            Template = JsonSerializer.Deserialize<HudBannerTemplate>(row.Template.DesignJson) ?? new(),
            Content = new() { Header = row.HeaderKey, Title = row.TitleKey, Description = row.DescriptionKey }
        };
        NotificationCatalog.Validate(rule);
        return rule;
    }

    internal static IQueryable<RuntimeSettings> BuildRuntimeSettingsQuery(
        AdvertisementDbContext context) =>
        context.Settings
            .AsNoTracking()
            .Select(entity => new RuntimeSettings(
                entity.Enabled,
                entity.IntervalSeconds,
                entity.RefreshIntervalSeconds,
                entity.InitialDelaySeconds,
                entity.OrderMode,
                entity.ExcludeBotsFromPlayers,
                entity.ConfigurationVersion));

    internal static AdvertisementMessage MapMessage(AdvertisementMessageEntity entity) => new(
        entity.Id, entity.Key, entity.Name, entity.LocalizationKey,
        string.IsNullOrWhiteSpace(entity.TagKey) ? null : entity.TagKey.Trim(),
        entity.Type, entity.Enabled,
        entity.Priority, entity.Weight, entity.SortOrder, entity.IntervalSeconds,
        DeliveryRuleParser.ParseDispatchMode(entity.DispatchMode),
        DeliveryRuleParser.ParseDailyTimesJson(entity.DailyTimesJson),
        entity.DailyStartTime, entity.DailyEndTime,
        DeliveryRuleParser.ParseAudienceType(entity.AudienceType),
        string.IsNullOrWhiteSpace(entity.AudienceGroup) ? null : entity.AudienceGroup.Trim(),
        entity.MinPlayers, entity.MaxPlayers, entity.StartsAt, entity.EndsAt)
    {
        Presentation = new(entity.DisplayType, entity.HudLocalizationKey,
            entity.HudPosition, entity.HudDurationSeconds, entity.HudStyle)
        {
            Template = entity.BannerTemplate is null ? null : JsonSerializer.Deserialize<HudBannerTemplate>(entity.BannerTemplate.DesignJson),
            HeaderKey = entity.BannerHeaderKey, TitleKey = entity.BannerTitleKey,
            Parameters = (JsonSerializer.Deserialize<Dictionary<string, string>>(entity.BannerParametersJson) ?? []).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
        }
    };
}
