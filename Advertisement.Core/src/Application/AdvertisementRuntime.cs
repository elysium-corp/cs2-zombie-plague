using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Admin.Api;
using Advertisement.Core.Data;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Application;

internal sealed class AdminAudienceResolver
{
    private IAdminApi? _adminApi;

    public void Initialize(IAdminApi adminApi)
    {
        _adminApi = adminApi;
    }

    public void Uninitialize()
    {
        _adminApi = null;
    }

    public IPlayer[] Resolve(AdvertisementMessage message, IEnumerable<IPlayer> players)
    {
        var targets = players
            .Where(player => player is { IsAuthorized: true, IsFakeClient: false })
            .ToArray();

        if (message.AudienceType == AdvertisementAudienceType.All)
        {
            return targets;
        }

        var group = message.AudienceGroup;
        var adminApi = _adminApi;
        if (adminApi is null || string.IsNullOrWhiteSpace(group))
        {
            return [];
        }

        return targets
            .Where(player => adminApi.GetPlayerPrivileges(player)
                .Any(privilege => string.Equals(privilege.Group, group, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}

internal sealed class RateLimitedLogger(ILogger logger)
{
    private const int MaximumKeys = 512;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _last = new(StringComparer.Ordinal);
    public void Warning(string key, TimeSpan interval, string message, params object?[] args)
    {
        var now = DateTimeOffset.UtcNow;
        if (_last.Count >= MaximumKeys && !_last.ContainsKey(key))
        {
            foreach (var stale in _last.Where(item => now - item.Value >= interval).Take(64))
                _last.TryRemove(stale.Key, out _);
            if (_last.Count >= MaximumKeys) key = "rate-limit:overflow";
        }
        var previous = _last.GetOrAdd(key, DateTimeOffset.MinValue);
        if (now - previous < interval || !_last.TryUpdate(key, now, previous)) return;
        logger.LogWarning(message, args);
    }
}

internal sealed partial class MarkupRenderer
{
    private static readonly HashSet<string> SupportedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "default","white","darkred","lightpurple","green","olive","lime","red","gray","grey",
        "lightyellow","yellow","silver","bluegrey","lightblue","blue","darkblue","purple","magenta",
        "lightred","gold","orange",
    };

    public string Render(string text, IReadOnlyDictionary<string, string> colors)
    {
        text = RawColorRegex().Replace(text, string.Empty);
        var output = new StringBuilder(text.Length + 32);
        var stack = new List<(string Name, string Color)> { ("root", ResolveColor("default", colors)) };
        var position = 0;
        foreach (Match match in MarkupRegex().Matches(text))
        {
            output.Append(text, position, match.Index - position);
            position = match.Index + match.Length;
            var name = match.Groups["name"].Value.ToLowerInvariant();
            if (match.Groups["close"].Success)
            {
                for (var i = stack.Count - 1; i > 0; i--)
                {
                    if (!string.Equals(stack[i].Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                    stack.RemoveRange(i, stack.Count - i);
                    output.Append('[').Append(stack[^1].Color).Append(']');
                    break;
                }
                continue;
            }
            var color = name == "color" ? NormalizeColor(match.Groups["color"].Value) : ResolveColor(name, colors);
            stack.Add((name, color));
            output.Append('[').Append(color).Append(']');
        }
        output.Append(text, position, text.Length - position).Append("[/]");
        return output.ToString();
    }

    public string NormalizeColor(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SupportedColors.Contains(value) ? value.ToLowerInvariant() : "default";
    private string ResolveColor(string name, IReadOnlyDictionary<string, string> colors) =>
        colors.TryGetValue(name, out var color) ? NormalizeColor(color) : "default";

    [GeneratedRegex(@"\{(?<close>/)?(?<name>accent|warning|success|important|muted|color)(?::(?<color>[a-z]+))?\}", RegexOptions.IgnoreCase)]
    private static partial Regex MarkupRegex();
    [GeneratedRegex(@"\[(?:/?|default|white|darkred|lightpurple|green|olive|lime|red|gr[ae]y|lightyellow|yellow|silver|bluegrey|lightblue|blue|darkblue|purple|magenta|lightred|gold|orange)\]", RegexOptions.IgnoreCase)]
    private static partial Regex RawColorRegex();
}

internal sealed partial class PlaceholderResolver
{
    public string Resolve(string template, IPlayer player, string serverName, string mapName, int players, int bots, int maxPlayers, string nextMap, DateTimeOffset now)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["server_name"] = serverName, ["map"] = mapName, ["players"] = players.ToString(),
            ["bots"] = bots.ToString(), ["total_players"] = (players + bots).ToString(),
            ["max_players"] = maxPlayers.ToString(), ["time"] = now.ToLocalTime().ToString("HH:mm"),
            ["next_map"] = nextMap, ["player_name"] = player.Name, ["steam_id"] = player.SteamID.ToString(),
        };
        return PlaceholderRegex().Replace(template, m => values.TryGetValue(m.Groups[1].Value, out var value) ? RawColorRegex().Replace(value, string.Empty) : m.Value);
    }
    [GeneratedRegex(@"\{([a-z_]+)\}", RegexOptions.IgnoreCase)] private static partial Regex PlaceholderRegex();
    [GeneratedRegex(@"\[(?:/?|default|white|darkred|lightpurple|green|olive|lime|red|gr[ae]y|lightyellow|yellow|silver|bluegrey|lightblue|blue|darkblue|purple|magenta|lightred|gold|orange)\]", RegexOptions.IgnoreCase)] private static partial Regex RawColorRegex();
}

internal sealed class AdvertisementSender(
    Func<ILocalizationApi> localization,
    PlaceholderResolver placeholderResolver,
    MarkupRenderer markupRenderer)
{
    public void Send(AdvertisementSnapshot snapshot, AdvertisementMessage message, IEnumerable<IPlayer> targets,
        int humans, int bots, string serverName, string mapName, string nextMap, int maxPlayers,
        DateTimeOffset now, AdvertisementTag? tag, string? localeOverride = null)
    {
        foreach (var player in targets)
        {
            if (player.IsFakeClient || !player.IsAuthorized) continue;
            var text = localeOverride is null
                ? localization().GetForPlayer(player, message.LocalizationKey)
                : localization().GetForLanguage(localeOverride, message.LocalizationKey);
            if (text is null) continue;

            var resolved = placeholderResolver.Resolve(text, player, serverName, mapName,
                snapshot.Settings.ExcludeBotsFromPlayers ? humans : humans + bots, bots, maxPlayers, nextMap, now);
            var output = new StringBuilder(resolved.Length + 48);
            if (tag is { Enabled: true })
            {
                var tagText = localeOverride is null
                    ? localization().GetForPlayer(player, tag.LocalizationKey)
                    : localization().GetForLanguage(localeOverride, tag.LocalizationKey);
                if (!string.IsNullOrWhiteSpace(tagText))
                    output.Append('[').Append(markupRenderer.NormalizeColor(tag.Color)).Append("][").Append(tagText).Append("][/] ");
            }
            output.Append(markupRenderer.Render(resolved, snapshot.Settings.Colors));
            player.SendMessage(MessageType.Chat, output.ToString().Colored());
        }
    }

}

internal sealed class AdvertisementScheduler(
    ISwiftlyCore core,
    AdvertisementCache cache,
    AdvertisementSender sender,
    AdminAudienceResolver audienceResolver)
{
    private readonly ConcurrentDictionary<long, DateTimeOffset> _lastSent = new();
    private readonly ConcurrentDictionary<string, byte> _dailyOccurrences = new(StringComparer.Ordinal);
    private DateTimeOffset _nextDispatchAt = DateTimeOffset.MaxValue;
    private DateOnly _dailyDate;
    private int _sequence;
    private string _mapName = string.Empty;

    public string CurrentMapName => _mapName;

    public bool TryStartFromCurrentMap()
    {
        try
        {
            var mapName = core.Engine.GlobalVars.MapName.Value;
            if (string.IsNullOrWhiteSpace(mapName)) return false;

            OnMapLoaded(mapName);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void OnMapLoaded(string mapName)
    {
        _mapName = mapName; _sequence = 0; _lastSent.Clear();
        _nextDispatchAt = DateTimeOffset.UtcNow.AddSeconds(cache.Current?.Settings.InitialDelaySeconds ?? 45);
    }

    public void Tick()
    {
        var snapshot = cache.Current;
        var now = DateTimeOffset.UtcNow;
        if (snapshot is null || !snapshot.Settings.Enabled || string.IsNullOrWhiteSpace(_mapName)) return;

        var players = core.PlayerManager.GetAllPlayers().Where(x => x.IsAuthorized).ToArray();
        var bots = players.Count(x => x.IsFakeClient);
        var humans = players.Length - bots;
        var conditionCount = snapshot.Settings.ExcludeBotsFromPlayers ? humans : players.Length;

        DispatchDaily(snapshot, players, humans, bots, conditionCount, now);

        if (now < _nextDispatchAt)
        {
            return;
        }

        var candidates = snapshot.Messages.Values
            .Where(x => x.DispatchMode == AdvertisementDispatchMode.Periodic)
            .Where(x => x.IsActive(now, conditionCount))
            .Where(x => !_lastSent.TryGetValue(x.Id, out var last) || now - last >= TimeSpan.FromSeconds(x.IntervalSeconds ?? snapshot.Settings.IntervalSeconds))
            .Where(x => audienceResolver.Resolve(x, players).Length > 0)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id).ToArray();
        if (candidates.Length == 0) { _nextDispatchAt = now.AddSeconds(Math.Min(10, snapshot.Settings.IntervalSeconds)); return; }

        var message = Select(candidates, snapshot.Settings.OrderMode);
        var targets = audienceResolver.Resolve(message, players);
        Send(snapshot, message, targets, humans, bots, now, ResolveConfiguredTag(snapshot, message));
        _lastSent[message.Id] = now;
        _nextDispatchAt = now.AddSeconds(message.IntervalSeconds ?? snapshot.Settings.IntervalSeconds);
    }

    public void SendTest(AdvertisementMessage message, IPlayer player, string? locale)
    {
        var snapshot = cache.Current; if (snapshot is null) return;
        var all = core.PlayerManager.GetAllPlayers().Where(x => x.IsAuthorized).ToArray();
        var bots = all.Count(x => x.IsFakeClient); var humans = all.Length - bots;
        sender.Send(snapshot, message, [player], humans, bots,
            core.ConVar.FindAsString("hostname")?.ValueAsString ?? "Elysium", _mapName,
            core.ConVar.FindAsString("nextlevel")?.ValueAsString ?? string.Empty,
            core.PlayerManager.MaxPlayers, DateTimeOffset.UtcNow,
            ResolveConfiguredTag(snapshot, message), locale);
    }

    public bool SendManual(AdvertisementMessage message, IEnumerable<IPlayer> targets, string? tagKey)
    {
        var snapshot = cache.Current;
        if (snapshot is null)
        {
            return false;
        }

        AdvertisementTag? tag;
        if (tagKey is null)
        {
            tag = ResolveConfiguredTag(snapshot, message);
        }
        else
        {
            tag = snapshot.Tags.Values.FirstOrDefault(value =>
                value.Enabled && value.Key.Equals(tagKey, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                return false;
            }
        }

        var all = core.PlayerManager.GetAllPlayers().Where(x => x.IsAuthorized).ToArray();
        var bots = all.Count(x => x.IsFakeClient);
        var humans = all.Length - bots;
        Send(snapshot, message, targets, humans, bots, DateTimeOffset.UtcNow, tag);
        return true;
    }

    private void DispatchDaily(
        AdvertisementSnapshot snapshot,
        IPlayer[] players,
        int humans,
        int bots,
        int conditionCount,
        DateTimeOffset now)
    {
        var local = now.LocalDateTime;
        var currentDate = DateOnly.FromDateTime(local);
        if (_dailyDate != currentDate)
        {
            _dailyDate = currentDate;
            _dailyOccurrences.Clear();
        }

        foreach (var message in snapshot.Messages.Values
                     .Where(x => x.DispatchMode == AdvertisementDispatchMode.Daily)
                     .Where(x => x.IsActive(now, conditionCount))
                     .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            var scheduledTime = message.DailyTimes
                .Where(time => time.Hour == local.Hour && time.Minute == local.Minute)
                .Select(time => (TimeOnly?)time)
                .FirstOrDefault();
            if (scheduledTime is null)
            {
                continue;
            }

            var occurrence = $"{message.Id}:{currentDate:yyyyMMdd}:{scheduledTime.Value:HHmm}";
            if (_dailyOccurrences.ContainsKey(occurrence))
            {
                continue;
            }

            var targets = audienceResolver.Resolve(message, players);
            if (targets.Length == 0 || !_dailyOccurrences.TryAdd(occurrence, 0))
            {
                continue;
            }

            Send(snapshot, message, targets, humans, bots, now, ResolveConfiguredTag(snapshot, message));
        }
    }

    private void Send(
        AdvertisementSnapshot snapshot,
        AdvertisementMessage message,
        IEnumerable<IPlayer> targets,
        int humans,
        int bots,
        DateTimeOffset now,
        AdvertisementTag? tag)
    {
        sender.Send(snapshot, message, targets, humans, bots,
            core.ConVar.FindAsString("hostname")?.ValueAsString ?? "Elysium", _mapName,
            core.ConVar.FindAsString("nextlevel")?.ValueAsString ?? string.Empty,
            core.PlayerManager.MaxPlayers, now, tag);
    }

    private static AdvertisementTag? ResolveConfiguredTag(
        AdvertisementSnapshot snapshot,
        AdvertisementMessage message)
    {
        return message.TagId is { } tagId
               && snapshot.Tags.TryGetValue(tagId, out var tag)
               && tag.Enabled
            ? tag
            : null;
    }

    private AdvertisementMessage Select(AdvertisementMessage[] items, AdvertisementOrderMode mode)
    {
        if (mode == AdvertisementOrderMode.Random) return items[Random.Shared.Next(items.Length)];
        if (mode == AdvertisementOrderMode.WeightedRandom)
        {
            var total = items.Sum(x => Math.Max(0, x.Weight));
            if (total > 0)
            {
                var roll = Random.Shared.Next(total);
                foreach (var item in items) { roll -= Math.Max(0, item.Weight); if (roll < 0) return item; }
            }
        }
        return items[Math.Abs(Interlocked.Increment(ref _sequence) - 1) % items.Length];
    }
}

internal sealed class AdvertisementCoordinator(
    AdvertisementCache cache,
    DatabaseAdvertisementProvider databaseProvider,
    ConfigAdvertisementProvider configProvider,
    RateLimitedLogger rateLimitedLogger,
    ILogger logger) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly HashSet<Task> _tasks = [];
    private readonly object _taskSync = new();
    private int _started;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        cache.Replace(configProvider.Load());
        Track(ReloadDatabaseAsync(_lifetime.Token, "запуск плагина"));
    }

    public Task<(bool Success, string Message)> ReloadNowAsync()
    {
        var task = ReloadDatabaseAsync(_lifetime.Token, "команда ads_reload");
        Track(task);
        return task;
    }

    public Task<(bool Success, string Message)> ReloadForMapAsync(string mapName)
    {
        var task = ReloadDatabaseAsync(_lifetime.Token, $"смена карты на {mapName}");
        Track(task);
        return task;
    }

    private async Task<(bool Success, string Message)> ReloadDatabaseAsync(
        CancellationToken token,
        string reason)
    {
        try
        {
            await _reloadLock.WaitAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return (false, "Advertisement reload cancelled.");
        }

        try
        {
            var snapshot = await databaseProvider.LoadAsync(token);
            cache.Replace(snapshot);
            logger.LogInformation(
                "[Advertisement] Загружено {Messages} сообщений из PostgreSQL. Причина: {Reason}.",
                snapshot.Messages.Count,
                reason);
            return (true, $"Snapshot обновлён: {snapshot.Messages.Count} сообщений.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return (false, "Advertisement reload cancelled.");
        }
        catch (Exception ex)
        {
            cache.MarkDatabaseUnavailable();
            rateLimitedLogger.Warning("database:unavailable", TimeSpan.FromMinutes(2),
                "[Advertisement] PostgreSQL недоступен: {Error}. Текущий snapshot сохранён.", ex.Message);
            return (false, "Reload failed. Current cache preserved.");
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void Track(Task task)
    {
        lock (_taskSync)
        {
            _tasks.Add(task);
        }

        _ = task.ContinueWith(
            completed =>
            {
                lock (_taskSync)
                {
                    _tasks.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        Task[] tasks;
        lock (_taskSync)
        {
            tasks = _tasks.ToArray();
        }

        try
        {
            Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }
    }
}
