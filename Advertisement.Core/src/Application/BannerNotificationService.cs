using System.Security.Cryptography;
using System.Text;
using Advertisement.Core.Data;
using CustomHud.Api;
using Economy.Api;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Application;

internal sealed class BannerNotificationService(ISwiftlyCore core, AdvertisementCache cache,
    Func<ILocalizationApi> localization, TimeProvider clock) : IBannerNotificationApi, IDisposable
{
    private readonly NotificationQueue _queue = new(clock);
    private readonly Dictionary<string, (object Token, Func<IPlayer, IReadOnlyDictionary<string, object?>> Provider)> _providers = [];
    private readonly HashSet<Action> _configurationListeners = [];
    private readonly Dictionary<string, DateTimeOffset> _warnings = [];
    private ICustomBannerApi? _banners;
    private ICustomHudApi? _hud;
    private IEconomyApi? _economy;
    private AdvertisementSnapshot? _snapshot;
    private CancellationTokenSource? _timer;
    private bool _disposed;

    internal void Initialize(ICustomBannerApi? banners, ICustomHudApi? hud, IEconomyApi? economy)
    {
        if (!ReferenceEquals(_hud, hud)) Reset();
        _banners = banners; _hud = hud; _economy = economy;
    }

    internal void Start()
    {
        _timer = core.Scheduler.RepeatBySeconds(.1f, Tick);
        core.Event.OnMapUnload += MapUnload;
        core.Event.OnClientDisconnected += Disconnect;
        core.Event.OnClientConnected += Connect;
    }

    public bool Publish(IPlayer player, string eventKey, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ObserveConfiguration();
        if (_disposed || _banners is not { IsAvailable: true } || !Eligible(player)
            || _snapshot is null || !_snapshot.Notifications.TryGetValue(eventKey, out var rule) || !rule.Enabled
            || !_queue.CanAccept(player.PlayerID, player.SteamID, rule)) return false;
        try
        {
            var values = Context(player);
            if (parameters is not null)
                foreach (var pair in parameters) values[pair.Key] = NotificationCatalog.Scalar(pair.Value);
            foreach (var pair in rule.Parameters) values.TryAdd(pair.Key, NotificationCatalog.Scalar(pair.Value));
            // Алиасы разрешаются после подстановки контекста события, чтобы player_name совпадал с player.
            foreach (var (name, aliases) in NotificationCatalog.Aliases)
                if (values.TryGetValue(name, out var value)) foreach (var alias in aliases) values[alias] = value;
            if (!Allowed(player, rule, values)) return false;
            return _queue.Enqueue(player.PlayerID, player.SteamID, rule, values);
        }
        catch (Exception exception) { Warn(eventKey, exception); return false; }
    }

    public int Broadcast(string eventKey, IReadOnlyDictionary<string, object?>? parameters = null) =>
        core.PlayerManager.GetAllPlayers().Count(player => Publish(player, eventKey, parameters));

    public IDisposable RegisterContext(string owner, Func<IPlayer, IReadOnlyDictionary<string, object?>> provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner); ArgumentNullException.ThrowIfNull(provider);
        var token = new object();
        _providers[owner] = (token, provider);
        return new Registration(() => { if (ReferenceEquals(_providers.GetValueOrDefault(owner).Token, token)) _providers.Remove(owner); });
    }

    public HudWidgetOptions? GetWidget(string key) => cache.Current?.Widgets.GetValueOrDefault(key);
    public IDisposable SubscribeConfiguration(Action callback)
    {
        _configurationListeners.Add(callback);
        return new Registration(() => _configurationListeners.Remove(callback));
    }

    public void Clear(string eventKey) { _queue.ClearEvent(eventKey); _hud?.ClearChannel(Channel(eventKey)); }

    internal Dictionary<string, object?> Context(IPlayer player)
    {
        var players = core.PlayerManager.GetAllPlayers().Where(Eligible).ToArray();
        var bots = core.PlayerManager.GetAllPlayers().Count(item => item.IsValid && item.IsFakeClient);
        var stats = player.Controller.ActionTrackingServices?.MatchStats;
        var now = clock.GetLocalNow();
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["player"] = player.Name, ["recipient"] = player.Name, ["steam_id"] = player.SteamID.ToString(),
            ["map"] = core.Engine.GlobalVars.MapName.Value, ["server_name"] = core.ConVar.FindAsString("hostname")?.ValueAsString ?? "",
            ["next_map"] = core.ConVar.FindAsString("nextlevel")?.ValueAsString ?? "", ["team"] = player.Controller.Team.ToString(),
            ["time"] = now.ToString("HH:mm"), ["date"] = now.ToString("yyyy-MM-dd"), ["language"] = localization().Resolve(player),
            ["round"] = (core.EntitySystem.GetGameRules()?.TotalRoundsPlayed ?? 0) + 1,
            ["players"] = players.Length, ["max_players"] = core.PlayerManager.MaxPlayers, ["bots"] = bots,
            ["total_players"] = players.Length + bots, ["alive_players"] = players.Count(item => item.IsAlive),
            ["health"] = player.PlayerPawn?.Health ?? 0, ["armor"] = player.PlayerPawn?.ArmorValue ?? 0,
            ["is_alive"] = player.IsAlive, ["kills"] = stats?.Kills ?? 0, ["deaths"] = stats?.Deaths ?? 0,
            ["assists"] = stats?.Assists ?? 0, ["score"] = player.Controller.Score, ["mvps"] = player.Controller.MVPs
        };
        if (_economy is not null) values["balance"] = _economy.GetBalance(player);
        foreach (var (owner, provider) in _providers.ToArray())
        {
            try { foreach (var pair in provider.Provider(player)) values[pair.Key] = pair.Value; }
            catch (Exception exception) { Warn("context:" + owner, exception); }
        }
        return values;
    }

    private bool Allowed(IPlayer player, BannerNotificationRule rule, IReadOnlyDictionary<string, object?> values) =>
        Convert.ToInt32(values["players"]) >= rule.MinPlayers && (rule.Audience switch
        {
            "alive" => player.IsAlive, "dead" => !player.IsAlive,
            "humans" => values.GetValueOrDefault("is_zombie") is false,
            "zombies" => values.GetValueOrDefault("is_zombie") is true,
            "spectators" => player.Controller.Team == Team.Spectator, _ => true
        });

    private void Tick()
    {
        if (_disposed) return;
        ObserveConfiguration();
        foreach (var pending in _queue.Ready())
        {
            var player = core.PlayerManager.GetPlayer(pending.PlayerId);
            if (player?.SteamID != pending.SteamId || !Eligible(player) || _banners is not { IsAvailable: true })
            { _queue.Disconnect(pending.PlayerId); continue; }
            var rule = pending.Rule;
            try
            {
                if (!Allowed(player, rule, Context(player))) { _queue.Reject(pending); continue; }
                var template = pending.IsUpdate ? rule.Template with { Enter = "none", Sound = null } : rule.Template;
                var accepted = _banners.ShowLocalized(player, template, rule.Content, pending.Parameters,
                    rule.Options with { Channel = Channel(rule.EventKey) });
                if (accepted)
                {
                    if (pending.PreviousEvent is { } previous && previous != rule.EventKey) _hud?.Hide(player, Channel(previous));
                    _queue.Shown(pending);
                }
                else { _queue.Reject(pending); Warn(rule.EventKey, new InvalidOperationException("Localization или HUD не приняли уведомление")); }
            }
            catch (Exception exception) { _queue.Reject(pending); Warn(rule.EventKey, exception); }
        }
    }

    private static bool Eligible(IPlayer? player) => player is { IsValid: true, IsAuthorized: true, IsFakeClient: false };
    private static string Channel(string key) => "Notifications." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..24];
    private void ObserveConfiguration()
    {
        var current = cache.Current;
        if (ReferenceEquals(current, _snapshot)) return;
        Reset(); _snapshot = current;
        foreach (var callback in _configurationListeners.ToArray())
            try { callback(); } catch (Exception exception) { Warn("widget", exception); }
    }
    private void Reset()
    {
        foreach (var key in _queue.EventKeys) _hud?.ClearChannel(Channel(key));
        _queue.Reset();
    }
    private void Warn(string key, Exception exception)
    {
        var now = clock.GetUtcNow();
        if (_warnings.TryGetValue(key, out var time) && now - time < TimeSpan.FromMinutes(2)) return;
        _warnings[key] = now;
        core.Logger.LogWarning(exception, "[Notifications] Не удалось показать {Event}", key);
    }
    private void MapUnload(IOnMapUnloadEvent args) => Reset();
    private void Disconnect(IOnClientDisconnectedEvent args) => _queue.Disconnect(args.PlayerId);
    private void Connect(IOnClientConnectedEvent args) => _queue.Disconnect(args.PlayerId);
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _timer?.Cancel(); _timer?.Dispose();
        core.Event.OnMapUnload -= MapUnload; core.Event.OnClientDisconnected -= Disconnect; core.Event.OnClientConnected -= Connect;
        Reset(); _providers.Clear(); _configurationListeners.Clear(); _banners = null; _hud = null; _economy = null;
    }
    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
