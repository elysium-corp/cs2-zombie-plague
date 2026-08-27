using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Advertisement.Core.Data;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Application;

internal sealed class PlayerLocaleStore
{
    private readonly ConcurrentDictionary<ulong, string?> _locales = new();
    private readonly ConcurrentDictionary<int, ulong> _slots = new();

    public void BindSlot(int playerId, ulong steamId) => _slots[playerId] = steamId;
    public void RemoveSlot(int playerId)
    {
        if (_slots.TryRemove(playerId, out var steamId)) _locales.TryRemove(steamId, out _);
    }
    public void Set(ulong steamId, string? locale) =>
        _locales[steamId] = string.IsNullOrWhiteSpace(locale) ? null : LocaleNormalizer.Normalize(locale);
    public bool TryGet(ulong steamId, out string? locale) => _locales.TryGetValue(steamId, out locale);
}

internal sealed class PlayerLocaleResolver(PlayerLocaleStore store)
{
    public string Resolve(IPlayer player, AdvertisementSettings settings)
    {
        if (store.TryGet(player.SteamID, out var manual) && !string.IsNullOrWhiteSpace(manual))
            return settings.AllowedLocales.Contains(manual) ? manual : settings.DefaultLocale;
        var engine = LocaleNormalizer.Normalize(player.PlayerLanguage.Value);
        return settings.AllowedLocales.Contains(engine) ? engine : settings.DefaultLocale;
    }
}

internal sealed class RateLimitedLogger(ILogger logger)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _last = new(StringComparer.Ordinal);
    public void Warning(string key, TimeSpan interval, string message, params object?[] args)
    {
        var now = DateTimeOffset.UtcNow;
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
    PlayerLocaleResolver localeResolver,
    PlaceholderResolver placeholderResolver,
    MarkupRenderer markupRenderer)
{
    public void Send(AdvertisementSnapshot snapshot, AdvertisementMessage message, IEnumerable<IPlayer> targets,
        int humans, int bots, string serverName, string mapName, string nextMap, int maxPlayers,
        DateTimeOffset now, string? localeOverride = null)
    {
        foreach (var player in targets)
        {
            if (player.IsFakeClient || !player.IsAuthorized) continue;
            var locale = localeOverride is null ? localeResolver.Resolve(player, snapshot.Settings) : LocaleNormalizer.Normalize(localeOverride);
            var text = ResolveTranslation(message.Translations, locale, snapshot.Settings.DefaultLocale);
            if (text is null) continue;

            var resolved = placeholderResolver.Resolve(text, player, serverName, mapName,
                snapshot.Settings.ExcludeBotsFromPlayers ? humans : humans + bots, bots, maxPlayers, nextMap, now);
            var output = new StringBuilder(resolved.Length + 48);
            if (message.TagId is { } tagId && snapshot.Tags.TryGetValue(tagId, out var tag) && tag.Enabled)
            {
                var tagText = ResolveTranslation(tag.Translations, locale, snapshot.Settings.DefaultLocale);
                if (!string.IsNullOrWhiteSpace(tagText))
                    output.Append('[').Append(markupRenderer.NormalizeColor(tag.Color)).Append("][").Append(tagText).Append("][/] ");
            }
            output.Append(markupRenderer.Render(resolved, snapshot.Settings.Colors));
            player.SendMessage(MessageType.Chat, output.ToString().Colored());
        }
    }

    private static string? ResolveTranslation(FrozenDictionary<string, string> translations, string locale, string fallback) =>
        translations.TryGetValue(locale, out var value) ? value : translations.GetValueOrDefault(fallback);
}

internal sealed class AdvertisementScheduler(ISwiftlyCore core, AdvertisementCache cache, AdvertisementSender sender)
{
    private readonly ConcurrentDictionary<long, DateTimeOffset> _lastSent = new();
    private DateTimeOffset _nextDispatchAt = DateTimeOffset.MaxValue;
    private int _sequence;
    private string _mapName = string.Empty;

    public void StartFromCurrentMap() => OnMapLoaded(core.Engine.GlobalVars.MapName.Value);
    public void OnMapLoaded(string mapName)
    {
        _mapName = mapName; _sequence = 0; _lastSent.Clear();
        _nextDispatchAt = DateTimeOffset.UtcNow.AddSeconds(cache.Current?.Settings.InitialDelaySeconds ?? 45);
    }

    public void Tick()
    {
        var snapshot = cache.Current;
        var now = DateTimeOffset.UtcNow;
        if (snapshot is null || !snapshot.Settings.Enabled || now < _nextDispatchAt) return;
        var players = core.PlayerManager.GetAllPlayers().Where(x => x.IsAuthorized).ToArray();
        var bots = players.Count(x => x.IsFakeClient);
        var humans = players.Length - bots;
        var conditionCount = snapshot.Settings.ExcludeBotsFromPlayers ? humans : players.Length;
        var candidates = snapshot.Messages.Values.Where(x => x.IsActive(now, conditionCount))
            .Where(x => !_lastSent.TryGetValue(x.Id, out var last) || now - last >= TimeSpan.FromSeconds(x.IntervalSeconds ?? snapshot.Settings.IntervalSeconds))
            .OrderByDescending(x => x.Priority).ThenBy(x => x.SortOrder).ThenBy(x => x.Id).ToArray();
        if (candidates.Length == 0) { _nextDispatchAt = now.AddSeconds(Math.Min(10, snapshot.Settings.IntervalSeconds)); return; }
        var message = Select(candidates, snapshot.Settings.OrderMode);
        sender.Send(snapshot, message, players, humans, bots,
            core.ConVar.FindAsString("hostname")?.ValueAsString ?? "Elysium", _mapName,
            core.ConVar.FindAsString("nextlevel")?.ValueAsString ?? string.Empty,
            core.PlayerManager.MaxPlayers, now);
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
            core.PlayerManager.MaxPlayers, DateTimeOffset.UtcNow, locale);
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
    private Task? _task;

    public void Start() => _task ??= RunAsync(_lifetime.Token);
    public Task<(bool Success, string Message)> ReloadNowAsync() => ReloadDatabaseAsync(_lifetime.Token);

    private async Task RunAsync(CancellationToken token)
    {
        cache.Replace(configProvider.Load());
        while (!token.IsCancellationRequested)
        {
            _ = await ReloadDatabaseAsync(token);
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, cache.Current?.Settings.RefreshIntervalSeconds ?? 30)), token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
        }
    }

    private async Task<(bool Success, string Message)> ReloadDatabaseAsync(CancellationToken token)
    {
        await _reloadLock.WaitAsync(token);
        try
        {
            var snapshot = await databaseProvider.LoadAsync(token);
            cache.Replace(snapshot);
            logger.LogInformation("[Advertisement] Загружено {Messages} сообщений из PostgreSQL.", snapshot.Messages.Count);
            return (true, $"Snapshot обновлён: {snapshot.Messages.Count} сообщений.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            cache.MarkDatabaseUnavailable();
            rateLimitedLogger.Warning("database:unavailable", TimeSpan.FromMinutes(2),
                "[Advertisement] PostgreSQL недоступен: {Error}. Текущий snapshot сохранён.", ex.Message);
            return (false, "Reload failed. Current cache preserved.");
        }
        finally { _reloadLock.Release(); }
    }

    public void Dispose() => _lifetime.Cancel();
}
