using System.Collections.Immutable;
using Common.Di.Diagnostics;
using CustomHud.Api;
using MapRotation.Api;
using MapRotation.Core.Database;
using MapRotation.Core.Domain;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace MapRotation.Core;

internal sealed class RotationCoordinator(ISwiftlyCore core, RotationEngine engine, RotationStore store,
    MapEngineAdapter maps, TimeProvider clock, RotationText text) : IDisposable
{
    private const string NominationChannel = "MapRotation.Nomination";
    private const string VoteChannel = "MapRotation.Vote";
    private const string ResultChannel = "MapRotation.Result";
    private const string CardChannel = "MapRotation.Card";
    private readonly List<Guid> _commands = [];
    private readonly Dictionary<int, (ulong Session, Guid Vote)> _seenVotes = [];
    private readonly Dictionary<int, (ulong Session, Guid Menu, string Channel)> _opened = [];
    private ICustomHudMenuApi? _menus;
    private ICustomBannerApi? _banners;
    private ICustomHudApi? _messages;
    private CancellationTokenSource? _timer;
    private Guid _roundEndHook;
    private Guid _roundStartHook;
    private bool _started;
    private bool _disposed;
    private bool _mapUnloading;
    private bool _loaded;
    private bool _hudSuspended;
    private DateTimeOffset? _resultUntil;
    private DateTimeOffset? _changeRequestedAt;
    private long _publishedRevision = -1;
    private Guid? _lastVote;
    private long? _requestedMap;
    private int _mapEpoch;

    public void Bind(ICustomHudMenuApi? menus, ICustomBannerApi? banners, ICustomHudApi? messages)
    {
        if (!ReferenceEquals(_menus, menus))
        {
            CloseMenus(); _seenVotes.Clear();
            _menus = menus;
        }
        _banners = banners; _messages = messages;
    }

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        engine.ChangeRequested += Change;
        engine.MapFinished += store.Record;
        engine.VoteFinished += OnVoteFinished;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientDisconnected += OnDisconnect;
        core.Event.OnClientConnected += OnConnect;
        _roundEndHook = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _roundStartHook = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        foreach (var name in new[] { "timeleft", "nextmap", "rtv", "nominate" })
            _commands.Add(core.Command.RegisterCommand(name, PlayerCommand, registerRaw: true));
        foreach (var name in new[] { "maprotation_status", "maprotation_reload", "maprotation_vote", "maprotation_setnext", "maprotation_change" })
            _commands.Add(core.Command.RegisterCommand(name, AdminCommand, registerRaw: true, permission: "maprotation.admin"));
        store.Start();
        _timer = core.Scheduler.RepeatBySeconds(1, Tick);
    }

    private void Tick()
    {
        if (_disposed || _mapUnloading || !store.Initialized) return;
        try
        {
            if (!_loaded)
            {
                Apply(store.Initial!.Configuration);
                engine.LoadMap(maps.CurrentMap, maps.WorkshopId, store.Initial.Checkpoint);
                _loaded = true; maps.OwnRotation();
            }
            if (store.TakeConfiguration() is { } configuration) Apply(configuration);
            RefreshPlayers();
            engine.Tick();
            if (_changeRequestedAt is { } requested && engine.State == RotationState.ChangingMap
                && clock.GetUtcNow() >= requested.AddSeconds(30) && _requestedMap is { } failed)
            {
                core.Logger.LogWarning("[MapRotation] Engine не начал загрузку карты за 30 секунд");
                engine.ChangeFailed(failed); _changeRequestedAt = null;
            }
            RefreshHud();
            Publish();
        }
        catch (Exception error) { core.Logger.LogError(error, "[MapRotation] Ошибка обновления состояния"); }
    }

    private void Apply(RotationConfiguration configuration)
    {
        var valid = configuration.Maps.Where(maps.IsValid).Select(map => map.Id).ToArray();
        engine.Configure(configuration, valid);
        if (!configuration.Maps.IsEmpty && valid.Length == 0)
            core.Logger.LogWarning("[MapRotation] Нет установленных карт в каталоге; заполните map_rotation.maps");
    }

    private IPlayer[] Players() => core.PlayerManager.GetAllPlayers().Where(player => player.IsValid && !player.IsFakeClient && player.SteamID != 0).ToArray();
    private bool Eligible(IPlayer player) => player.IsValid && player.SteamID != 0 && (!engine.Settings.ExcludeBots || !player.IsFakeClient)
        && (engine.Settings.IncludeSpectators || player.Controller.Team is Team.CT or Team.T);
    private void RefreshPlayers(int? excludeId = null)
    {
        var connected = core.PlayerManager.GetAllPlayers().Where(player => player.PlayerID != excludeId && player.IsValid).ToArray();
        var humans = connected.Count(player => !player.IsFakeClient && player.SteamID != 0);
        var present = connected.Where(player =>
            (!engine.Settings.ExcludeBots || !player.IsFakeClient)
            && (engine.Settings.IncludeSpectators || player.Controller.Team is Team.CT or Team.T)).ToArray();
        engine.SetPlayers(present.Where(player => !player.IsFakeClient && player.SteamID != 0).Select(player => player.SteamID), present.Length, humans);
    }

    private void PlayerCommand(ICommandContext context)
    {
        if (context.Sender is not { IsValid: true } player) return;
        if (!_loaded || _mapUnloading) { context.Reply(Text(player, "Loading")); return; }
        RefreshPlayers();
        if (!engine.RotationEnabled) { context.Reply(Text(player, "InactiveNoMaps")); return; }
        switch (context.CommandName.ToLowerInvariant())
        {
            case "timeleft":
                Card(player, Text(player, "TimeLeft"), engine.PauseReason == RotationPauseReason.NoMaps ? Text(player, "UnlimitedTime")
                    : engine.State == RotationState.FinalRound ? Text(player, "LastRound") : Duration(engine.TimeLeft), "clock"); break;
            case "nextmap":
                Card(player, Text(player, "NextMap"), engine.NextMap?.DisplayName ?? Text(player, "NotSelected"), "info"); break;
            case "rtv":
                if (engine.Vote is not null) { OpenVote(player, force: true); break; }
                var reply = engine.Rtv(player.SteamID);
                if (reply == RotationReply.Delay) Card(player, Text(player, "RtvDelay"), Duration(TimeSpan.FromSeconds(engine.RtvDelayRemaining)), "clock");
                else if (reply is RotationReply.Accepted or RotationReply.Duplicate)
                {
                    Card(player, Text(player, "RtvTitle"), Text(player, "RtvProgress",
                        ("votes", engine.RtvVotes.ToString()), ("required", engine.RtvRequired.ToString())), "info",
                        Text(player, "RtvRemaining", ("count", Math.Max(0, engine.RtvRequired - engine.RtvVotes).ToString())));
                    if (reply == RotationReply.Accepted) ChatAll("RtvAdded", ("player", player.Controller.PlayerName),
                        ("votes", engine.RtvVotes.ToString()), ("required", engine.RtvRequired.ToString()));
                }
                else Card(player, Text(player, "RtvTitle"), Text(player, reply.ToString()), "warning");
                RefreshHud(); break;
            case "nominate":
                if (context.Args.Length > 0)
                {
                    var map = Find(context.Args[0]);
                    var result = map is null ? RotationReply.InvalidMap : engine.Nominate(player.SteamID, map.Id);
                    Card(player, Text(player, "NominationTitle"), result == RotationReply.Accepted ? map!.DisplayName : Text(player, result.ToString()), "info");
                }
                else OpenNomination(player);
                break;
        }
        Publish();
    }

    private void AdminCommand(ICommandContext context)
    {
        if (context.CommandName == "maprotation_reload") { store.RequestReload(); context.Reply(Text(context.Sender, "Admin.ReloadQueued")); return; }
        if (context.CommandName == "maprotation_status")
        {
            context.Reply(System.Text.Json.JsonSerializer.Serialize(engine.GetStatus())); return;
        }
        if (!_loaded || _mapUnloading) { context.Reply(Text(context.Sender, "Loading")); return; }
        RefreshPlayers();
        if (!engine.RotationEnabled) { context.Reply(Text(context.Sender, "InactiveNoMaps")); return; }
        if (context.CommandName == "maprotation_vote") context.Reply(Text(context.Sender,
            engine.StartVote(NextMapSource.Admin) ? "Admin.VoteStarted" : "Admin.VoteUnavailable"));
        else
        {
            var map = context.Args.Length == 1 ? Find(context.Args[0]) : null;
            var accepted = map is not null && maps.IsValid(map) && engine.SetNext(map.Id, context.CommandName == "maprotation_change");
            context.Reply(accepted ? Text(context.Sender, "Admin.NextMapSet", ("map", map!.DisplayName))
                : Text(context.Sender, "Admin.InvalidMap"));
        }
        RefreshHud(); Publish();
    }
    private RotationMap? Find(string value) => engine.Configuration.Maps.FirstOrDefault(map =>
        string.Equals(map.Key, value, StringComparison.OrdinalIgnoreCase) || string.Equals(map.MapName, value, StringComparison.OrdinalIgnoreCase));

    private void OpenNomination(IPlayer player)
    {
        if (!Eligible(player)) { Card(player, Text(player, "NominationTitle"), Text(player, "NotEligible"), "warning"); return; }
        if (!engine.Settings.NominationsEnabled || engine.State != RotationState.Playing || engine.Source != NextMapSource.Provisional)
        { Card(player, Text(player, "NominationTitle"), Text(player, engine.Settings.NominationsEnabled ? "Locked" : "Disabled"), "warning"); return; }
        var menu = NominationMenu(player);
        Open(player, menu, action =>
        {
            if (action.Action != HudMenuAction.Select || !long.TryParse(action.ItemId, out var id)) return;
            var reply = engine.Nominate(player.SteamID, id);
            Card(player, Text(player, "NominationTitle"), reply == RotationReply.Accepted
                ? engine.Configuration.Maps.First(map => map.Id == id).DisplayName : Text(player, reply.ToString()), "info");
            Publish();
        });
    }
    private HudMenu NominationMenu(IPlayer player) => new(NominationChannel, Text(player, "NominationTitle"), Text(player, "NominationSubtitle"),
        engine.NominationMaps().OrderBy(map => map.SortOrder).ThenBy(map => map.DisplayName).Select(map => new HudMenuItem(map.Id.ToString(), map.DisplayName,
            Selected: engine.Nomination(player.SteamID) == map.Id)).ToImmutableArray(), new() { CloseOnSelect = true })
        { CloseText = Text(player, "Close"), Footer = engine.NominationMaps().IsEmpty ? Text(player, "NoMaps") : "" };

    private HudMenu VoteMenu(IPlayer player, VoteState vote) => new(VoteChannel, Text(player, "VoteTitle"), Text(player, "VoteSubtitle"),
        vote.Options.Select(map => new HudMenuItem(map.Id.ToString(), map.DisplayName,
            Description: vote.Votes.GetValueOrDefault(player.SteamID) == map.Id ? Text(player, "YourVote") : "",
            Badge: Text(player, "Votes", ("count", vote.Votes.Values.Count(id => id == map.Id).ToString())),
            Selected: vote.Votes.GetValueOrDefault(player.SteamID) == map.Id)).ToImmutableArray(),
        new() { Priority = HudMenuPriority.Critical })
        { CloseText = Text(player, "Close"), Status = Duration(vote.EndsAt - clock.GetUtcNow()) };

    private void OpenVote(IPlayer player, bool force = false)
    {
        if (engine.Vote is not { } vote || !Eligible(player)) return;
        if (!force && _seenVotes.TryGetValue(player.PlayerID, out var seen) && seen == (player.SessionId, vote.Id)) return;
        _seenVotes[player.PlayerID] = (player.SessionId, vote.Id);
        var session = player.SessionId; var steam = player.SteamID; var playerId = player.PlayerID;
        Open(player, VoteMenu(player, vote), action =>
        {
            if (action.Action != HudMenuAction.Select || !long.TryParse(action.ItemId, out var id)) return;
            var current = core.PlayerManager.GetPlayer(playerId);
            if (current is null || current.SessionId != session || current.SteamID != steam || !Eligible(current)) return;
            RefreshPlayers();
            if (engine.CastVote(steam, vote.Id, id)) { RefreshHud(); Publish(); }
        });
    }

    private void Open(IPlayer player, HudMenu menu, Action<HudMenuEvent> handler)
    {
        if (_menus?.Open(player, menu, handler) is { } id) _opened[player.PlayerID] = (player.SessionId, id, menu.Channel);
        else player.SendMessage(MessageType.Chat, text.WithChatTag(player, Text(player, "HudUnavailable")));
    }
    private void RefreshHud()
    {
        if (!engine.RotationEnabled)
        {
            if (!_hudSuspended)
            {
                CloseMenus(); _messages?.ClearChannel(CardChannel);
                _seenVotes.Clear(); _lastVote = null; _hudSuspended = true;
            }
            return;
        }
        _hudSuspended = false;
        if (_lastVote != engine.Vote?.Id)
        {
            _menus?.CloseChannel(NominationChannel); _menus?.CloseChannel(VoteChannel);
            if (engine.Vote is not null) ChatAll("VoteStarted");
            _lastVote = engine.Vote?.Id;
        }
        if (_resultUntil is { } until && clock.GetUtcNow() >= until)
        { _menus?.CloseChannel(ResultChannel); _resultUntil = null; }
        foreach (var player in Players())
        {
            if (!Eligible(player))
            {
                if (_opened.TryGetValue(player.PlayerID, out var old)) _menus?.Close(player, old.Menu);
                _opened.Remove(player.PlayerID); continue;
            }
            if (engine.Vote is { } vote) OpenVote(player);
            if (!_opened.TryGetValue(player.PlayerID, out var opened) || opened.Session != player.SessionId) continue;
            if (_menus?.IsOpen(player, opened.Menu) != true) { _opened.Remove(player.PlayerID); continue; }
            if (opened.Channel == VoteChannel && engine.Vote is { } active) _menus.Update(player, opened.Menu, VoteMenu(player, active));
            if (opened.Channel == NominationChannel)
            {
                if (engine.State != RotationState.Playing) _menus.Close(player, opened.Menu);
                else _menus.Update(player, opened.Menu, NominationMenu(player));
            }
        }
    }

    private void OnVoteFinished(VoteArchive result)
    {
        store.Record(engine.CurrentMap, result);
        _menus?.CloseChannel(VoteChannel);
        if (result.WinnerId is not { } id) return;
        var winner = engine.Configuration.Maps.FirstOrDefault(map => map.Id == id);
        if (winner is null) return;
        _resultUntil = clock.GetUtcNow().AddSeconds(8);
        foreach (var player in Players().Where(Eligible))
        {
            var voteCount = result.Vote.Votes.Values.Count(value => value == id);
            var menu = new HudMenu(ResultChannel, Text(player, "ResultTitle"), Text(player, "NextMap"),
                [new(winner.Id.ToString(), winner.DisplayName, Text(player, "Votes", ("count", voteCount.ToString())))],
                new() { Priority = HudMenuPriority.Critical, CloseOnSelect = true, ShowPagination = false })
            {
                View = HudMenuView.Result, CloseText = Text(player, "Close"),
                Footer = Text(player, engine.State == RotationState.FinalRound ? "LastRoundDescription" : "ScheduledResult")
            };
            Open(player, menu, _ => { });
        }
    }

    private void Card(IPlayer player, string title, string value, string icon, string description = "")
    {
        var shown = _banners?.Show(player, new HudBannerTemplate
        {
            Variant = "custom", ShowHeader = true, ShowTitle = true, ShowDescription = description.Length > 0,
            Icon = icon, Theme = "midnight", Accent = "purple", Align = "left", WidthPixels = 400,
            HeaderSize = 16, TitleSize = 28, DescriptionSize = 16, IconSize = 40,
            Enter = "fade", Exit = "fade", Speed = "fast"
        }, new() { Header = title, Title = value, Description = description }, new()
        { Channel = CardChannel, Position = HudPosition.TopLeft, DurationSeconds = 6, Format = HudTextFormat.PlainText }) == true;
        if (!shown) player.SendMessage(MessageType.Chat, text.WithChatTag(player, Text(player, "CardSummary",
            ("title", title), ("value", value), ("description", description)).Trim()));
    }

    private string Text(IPlayer? player, string key, params (string Name, string Value)[] parameters)
        => text.Get(player, key, parameters);
    private void ChatAll(string key, params (string Name, string Value)[] values)
    {
        foreach (var player in Players()) player.SendMessage(MessageType.Chat, text.WithChatTag(player, Text(player, key, values)));
    }
    internal static string Duration(TimeSpan value)
    {
        var seconds = Math.Max(0, (long)Math.Ceiling(value.TotalSeconds));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void Change(RotationMap map, bool forced)
    {
        if (forced) { core.Logger.LogWarning("[MapRotation] Превышен timeout последнего раунда"); ChatAll("ForcedChange"); }
        CloseMenus();
        var epoch = _mapEpoch;
        // Смена выполняется после завершения текущего game-event и его остальных обработчиков.
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (_disposed || _mapUnloading || epoch != _mapEpoch || engine.State != RotationState.ChangingMap) return;
            try { maps.Change(map); _changeRequestedAt = clock.GetUtcNow(); _requestedMap = map.Id; }
            catch (Exception error)
            {
                core.Logger.LogError(error, "[MapRotation] Не удалось сменить карту на {Map}", map.Key);
                engine.ChangeFailed(map.Id);
            }
            Publish();
        });
        Publish();
    }
    private void OnMapLoad(IOnMapLoadEvent args)
    {
        _mapEpoch++; _mapUnloading = false; _changeRequestedAt = null; _seenVotes.Clear(); _lastVote = null;
        if (!_loaded) return;
        // Событие загрузки означает новую сессию, включая повтор той же карты.
        engine.LoadMap(args.MapName, maps.WorkshopId, engine.Checkpoint() with { State = RotationState.ChangingMap });
        Apply(engine.Configuration); maps.OwnRotation(); RefreshPlayers(); Publish();
    }
    private void OnMapUnload(IOnMapUnloadEvent args)
    {
        _mapEpoch++; _mapUnloading = true; CloseMenus();
        if (_loaded) { engine.UnloadMap(); Publish(); }
    }
    private void OnDisconnect(IOnClientDisconnectedEvent args)
    {
        _opened.Remove(args.PlayerId); _seenVotes.Remove(args.PlayerId);
        if (_loaded && !_mapUnloading) { RefreshPlayers(args.PlayerId); Publish(); }
    }
    private void OnConnect(IOnClientConnectedEvent args)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "MapRotation.client_connected", args.PlayerId);
        _opened.Remove(args.PlayerId);
        _seenVotes.Remove(args.PlayerId);
    }
    private HookResult OnRoundEnd(EventRoundEnd args)
    {
        if (_loaded && !_mapUnloading && args.Reason != (int)SwiftlyS2.Shared.Natives.RoundEndReason.GameCommencing
            && core.EntitySystem.GetGameRules() is { WarmupPeriod: false })
        { RefreshPlayers(); engine.RoundEnded(); Publish(); }
        return HookResult.Continue;
    }
    private HookResult OnRoundStart(EventRoundStart args)
    {
        if (_loaded && !_mapUnloading) maps.OwnRotation();
        return HookResult.Continue;
    }
    private void Publish()
    {
        if (!_loaded || _publishedRevision == engine.Revision) return;
        store.Publish(engine.Configuration, engine.Checkpoint()); _publishedRevision = engine.Revision;
    }
    private void CloseMenus()
    {
        _menus?.CloseChannel(NominationChannel); _menus?.CloseChannel(VoteChannel); _menus?.CloseChannel(ResultChannel);
        _opened.Clear(); _resultUntil = null;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _timer?.Cancel(); _timer = null;
        if (_started)
        {
            core.Event.OnMapLoad -= OnMapLoad; core.Event.OnMapUnload -= OnMapUnload;
            core.Event.OnClientDisconnected -= OnDisconnect; core.Event.OnClientConnected -= OnConnect;
            core.GameEvent.Unhook(_roundEndHook); core.GameEvent.Unhook(_roundStartHook);
            foreach (var command in _commands) core.Command.UnregisterCommand(command);
        }
        engine.ChangeRequested -= Change; engine.MapFinished -= store.Record; engine.VoteFinished -= OnVoteFinished;
        CloseMenus(); _messages?.ClearChannel(CardChannel);
        Publish(); store.Dispose(); maps.Dispose();
    }
}
