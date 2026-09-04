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
    long ConfigurationVersion);

internal sealed record AdvertisementMessage(
    long Id,
    string Key,
    string Name,
    string LocalizationKey,
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
            messages[id] = new AdvertisementMessage(
                id, message.Key,
                string.IsNullOrWhiteSpace(message.Name) ? message.Key : message.Name,
                string.IsNullOrWhiteSpace(message.LocalizationKey)
                    ? $"advertisement.messages.{message.Key}"
                    : message.LocalizationKey.Trim(),
                NormalizeTagKey(message.Tag), message.Type, message.Enabled, message.Priority, Math.Max(0, message.Weight),
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
            ParseOrder(config.OrderMode), config.ExcludeBotsFromPlayers, 0);

        return new AdvertisementSnapshot(settings, messages.ToFrozenDictionary(),
            DateTimeOffset.UtcNow, AdvertisementSource.Config);
    }

    public static AdvertisementOrderMode ParseOrder(string? value) => value?.ToLowerInvariant() switch
    {
        "random" => AdvertisementOrderMode.Random,
        "weighted_random" => AdvertisementOrderMode.WeightedRandom,
        _ => AdvertisementOrderMode.Sequential,
    };

    private static string? NormalizeTagKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}

internal sealed class DatabaseAdvertisementProvider(IDbContextFactory<AdvertisementDbContext> contextFactory)
{
    public async Task<AdvertisementSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settingsEntity = await context.Settings.AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В advertisement.settings отсутствует настройка.");

        var messages = await context.Messages.AsNoTracking()
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
            AdvertisementSource.Database);
    }

    private static AdvertisementMessage MapMessage(AdvertisementMessageEntity entity) => new(
        entity.Id, entity.Key, entity.Name, entity.LocalizationKey,
        string.IsNullOrWhiteSpace(entity.TagKey) ? null : entity.TagKey.Trim().ToLowerInvariant(),
        entity.Type, entity.Enabled,
        entity.Priority, entity.Weight, entity.SortOrder, entity.IntervalSeconds,
        DeliveryRuleParser.ParseDispatchMode(entity.DispatchMode),
        DeliveryRuleParser.ParseDailyTimesJson(entity.DailyTimesJson),
        entity.DailyStartTime, entity.DailyEndTime,
        DeliveryRuleParser.ParseAudienceType(entity.AudienceType),
        string.IsNullOrWhiteSpace(entity.AudienceGroup) ? null : entity.AudienceGroup.Trim(),
        entity.MinPlayers, entity.MaxPlayers, entity.StartsAt, entity.EndsAt);
}
