using Common.Di;
using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api;
using Economy.Api;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SupplyBox.Api;
using SupplyBox.Api.Events;
using SupplyBox.Api.Events.Contexts;
using SupplyBox.Configuration;
using SupplyBox.Data;
using SupplyBox.Data.Entity;
using SupplyBox.Di;
using SupplyBox.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Api.Events.Contexts.Round;

namespace SupplyBox;

[PluginMetadata(Id = "SupplyBox.Core", Version = "1.0.1", Name = "[ZP] SupplyBox",
    Author = "illusion & fdrinv", Description = "Database-managed supply drops with Flute CMS integration")]
internal sealed class SupplyBox(ISwiftlyCore core) : Plugin<SupplyBoxModule>(core)
{
    internal const string EditorPermission = "supply_box.admin.edit";
    private readonly Lazy<SupplyBoxMapConfigService> _maps = GetRequiredServiceLazy<SupplyBoxMapConfigService>();
    private readonly Lazy<SupplyBoxMenuService> _menu = GetRequiredServiceLazy<SupplyBoxMenuService>();
    private readonly Lazy<SupplyBoxRewardService> _rewards = GetRequiredServiceLazy<SupplyBoxRewardService>();
    private readonly Lazy<IHookPublisher> _hooks = GetRequiredServiceLazy<IHookPublisher>();
    private readonly Lazy<ISupplyBoxEvents> _events = GetRequiredServiceLazy<ISupplyBoxEvents>();
    private readonly List<Guid> _commands = [];
    private readonly List<SupplyBoxEntity> _boxes = [];
    private CancellationTokenSource? _timer;
    private CancellationTokenSource? _refreshTimer;
    private Guid _roundEndHook;
    private Guid _restartHook;
    private IRound? _round;
    private int _roundNumber;
    private int _roundDrops;
    private int _mapDrops;
    private bool _ready;
    private string _lastStatus = "waiting_for_round";
    public static IZombiePlagueApi ZombiePlagueApi = null!;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaces) =>
        interfaces.AddSharedInterface<ISupplyBoxApi, SupplyBoxApi>(ISupplyBoxApi.SharedApiKey, new(_events.Value));

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaces)
    {
        ZombiePlagueApi = interfaces.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
        BindSharedInterface<ILocalizationApi>(interfaces, ILocalizationApi.SharedApiKey);
        BindSharedInterface<IEconomyApi>(interfaces, IEconomyApi.SharedApiKey);
        BindSharedInterface<ICustomEquipmentApi>(interfaces, ICustomEquipmentApi.SharedApiKey);
    }

    protected override void OnStart()
    {
        _maps.Value.Refresh();
        Core.Event.OnPrecacheResource += OnPrecache;
    }

    protected override void OnReady()
    {
        _ready = true;
        _roundEndHook = Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _restartHook = Core.GameEvent.HookPost<EventCsPreRestart>(OnRestart);
        ZombiePlagueApi.Events.Rounds.Started.Hook(OnRoundStarted);
        Core.Event.OnMapLoad += OnMapLoad;
        Core.Event.OnMapUnload += OnMapUnload;
        // Shared API уже готовы, но при старте сервера первая карта ещё не загружена.
        // Существующую карту подхватываем при reload; иначе ждём OnMapLoad.
        if (!SupplyBoxMapBootstrap.TryLoadCurrentMap(
                () => Core.Engine.GlobalVars.MapName.Value,
                _maps.Value.LoadConfig))
            _lastStatus = "waiting_for_map";
        _refreshTimer = Core.Scheduler.RepeatBySeconds(30, () => _maps.Value.Refresh());
        _commands.Add(Core.Command.RegisterCommand("supply", context =>
        {
            if (context.Sender is { } player) _menu.Value.ShowMainMenu(player);
        }, registerRaw: true, permission: EditorPermission));
        _commands.Add(Core.Command.RegisterCommand("supply_reload", context =>
        {
            _maps.Value.Refresh();
            context.Reply("SupplyBox: загрузка БД/fallback запущена. supply_status покажет источник и состояние.");
        }, registerRaw: true, permission: EditorPermission));
        _commands.Add(Core.Command.RegisterCommand("supply_status", context =>
            context.Reply($"SupplyBox: source={_maps.Value.Source}, version={_maps.Value.Current.Version}, map={_maps.Value.MapName}, points={_maps.Value.GetSnapshot().Count}, active={_boxes.Count}, round={_roundNumber}, state={_lastStatus}"),
            registerRaw: true, permission: EditorPermission));
    }

    protected override void OnUnload()
    {
        _refreshTimer?.Cancel(); _refreshTimer = null;
        StopRound();
        if (_ready)
        {
            ZombiePlagueApi.Events.Rounds.Started.Unhook(OnRoundStarted);
            Core.GameEvent.Unhook(_roundEndHook);
            Core.GameEvent.Unhook(_restartHook);
        }
        Core.Event.OnMapLoad -= OnMapLoad;
        Core.Event.OnMapUnload -= OnMapUnload;
        Core.Event.OnPrecacheResource -= OnPrecache;
        foreach (var command in _commands) Core.Command.UnregisterCommand(command);
        _commands.Clear();
        if (_menu.IsValueCreated) _menu.Value.Dispose();
        if (_maps.IsValueCreated) _maps.Value.Dispose();
    }

    private void OnPrecache(IOnPrecacheResourceEvent args)
    {
        args.AddItem("models/props/crates/cs2_drop_crate_01.vmdl");
        var document = _maps.Value.Current.Document;
        foreach (var model in document.BoxTypes.SelectMany(box => new[] { box.Model, box.ParachuteModel })
                     .Append(document.Settings.SupplyBoxModel).Append(document.Settings.ParachuteModel)
                     .Where(model => model.Length > 0).Distinct()) args.AddItem(model);
    }

    private void OnMapLoad(IOnMapLoadEvent args)
    {
        StopRound(); if (_menu.IsValueCreated) _menu.Value.ClearPreviews(); _roundNumber = 0; _mapDrops = 0;
        _maps.Value.LoadConfig(args.MapName);
    }
    private void OnMapUnload(IOnMapUnloadEvent args) { StopRound(); if (_menu.IsValueCreated) _menu.Value.ClearPreviews(); _maps.Value.Refresh(); }
    private HookResult OnRoundEnd(EventRoundEnd args) { StopRound(); return HookResult.Continue; }
    private HookResult OnRestart(EventCsPreRestart args) { StopRound(); _roundNumber = 0; _mapDrops = 0; return HookResult.Continue; }
    private void OnRoundStarted(ref RoundStartedContext context)
    {
        StopRound();
        _round = context.Round; _roundNumber++; _roundDrops = 0;
        _rewards.Value.ResetRound();
        Schedule(_maps.Value.Value.FirstDropDelaySeconds);
    }

    private void Schedule(int seconds)
    {
        _timer?.Cancel();
        if (_round is null) return;
        CancellationTokenSource? timer = null;
        timer = Core.Scheduler.DelayBySeconds(seconds, () =>
        {
            if (_round is null || !ReferenceEquals(_timer, timer)) return;
            _timer = null;
            try { TryDrop(); }
            catch (Exception exception) { _lastStatus = "spawn_error"; Core.Logger.LogError(exception, "[SupplyBox] Drop failed; scheduler will retry."); }
            finally
            {
                if (_round is not null)
                {
                    var settings = _maps.Value.Value;
                    Schedule(_lastStatus is "loading" or "discovering_points" ? 3
                        : settings.RespawnTimeBySeconds + Random.Shared.Next(settings.TimeSpreadBySeconds + 1));
                }
            }
        });
        _timer = timer;
    }

    private void TryDrop()
    {
        var round = _round;
        var service = _maps.Value;
        _boxes.RemoveAll(box => !box.IsAlive);
        if (service.Source == "loading") { _lastStatus = "loading"; return; }
        var document = service.Current.Document;
        var settings = document.Settings;
        var map = service.GetMap();
        if (map is null && settings.AutoDiscoverSpawnPoints)
        {
            var positions = Core.EntitySystem.GetAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist")
                .Where(entity => entity.IsValidEntity && entity.AbsOrigin.HasValue).Take(64)
                .Select((entity, index) => new SupplyBoxPoint { Id = index + 1, Name = $"CT spawn {index + 1}",
                    X = entity.AbsOrigin!.Value.X, Y = entity.AbsOrigin!.Value.Y, Z = entity.AbsOrigin!.Value.Z }).ToArray();
            service.DiscoverPoints(service.MapName, positions);
            _lastStatus = positions.Length > 0 ? "discovering_points" : "no_spawn_points";
            return;
        }
        if (!settings.Enabled || map is not { Enabled: true }) { Reject("disabled"); return; }
        if (!SupplyBoxRules.RoundAllows(settings, _roundNumber, ZombiePlagueApi.IsSurvivorRound(_round!), ZombiePlagueApi.IsNemesisRound(_round!)))
        { Reject("round_conditions"); return; }
        var players = Core.PlayerManager.GetAllPlayers().Where(player => player.IsValid
            && (settings.CountBots || !player.IsFakeClient) && player.Controller.Team is Team.T or Team.CT).ToArray();
        if (!SupplyBoxRules.PopulationAllows(settings, players.Length,
            players.Count(player => player.IsAlive && !ZombiePlagueApi.IsInfected(player)),
            players.Count(player => player.IsAlive && ZombiePlagueApi.IsInfected(player))))
        { Reject("player_conditions"); return; }
        if (Random.Shared.Next(100) >= (map.ChanceDrop ?? settings.ChanceDrop))
        { Reject("chance_missed", SupplyBoxSpawnRejectionReason.ChanceMissed); return; }
        for (var count = 0; count < settings.BoxesPerWave; count++)
        {
            if (SupplyBoxRules.LimitReached(settings, map, _boxes.Count, _roundDrops, _mapDrops))
            { Reject("drop_limit", SupplyBoxSpawnRejectionReason.ActiveLimitReached); return; }
            var types = document.BoxTypes.Where(type => type.Enabled && type.Loot.Any(loot => loot.Enabled)).ToArray();
            var points = map.Points.Where(point => point.Enabled && _boxes.All(box => box.Index != point.Id)
                && types.Any(type => point.BoxType == "" || type.Key == point.BoxType)).ToArray();
            if (points.Length == 0) { Reject("no_available_points", SupplyBoxSpawnRejectionReason.SpawnPointUnavailable); return; }
            var point = SupplyBoxRewardService.Weighted(points, item => item.Weight);
            var type = SupplyBoxRewardService.Weighted(types.Where(type => point.BoxType == "" || type.Key == point.BoxType).ToArray(), item => item.Weight);
            var pre = new SupplyBoxSpawningContext(_boxes.Cast<ISupplyBoxEntity>().ToArray());
            if (!_hooks.Value.DispatchCancellable(ref pre)) { Reject("cancelled", SupplyBoxSpawnRejectionReason.Cancelled); return; }
            if (!ReferenceEquals(_round, round)) return;
            var box = DependencyResolver.GetRequiredService<SupplyBoxEntity>();
            try { if (!box.Spawn(point, type)) { box.Dispose(); return; } }
            catch { box.Dispose(); throw; }
            _boxes.Add(box); _roundDrops++; _mapDrops++; _lastStatus = "spawned";
            var post = new SupplyBoxSpawnedContext(box); _hooks.Value.Dispatch(ref post);
        }
    }

    private void Reject(string status, SupplyBoxSpawnRejectionReason reason = SupplyBoxSpawnRejectionReason.RoundNotSupported)
    {
        _lastStatus = status;
        var rejected = new SupplyBoxSpawnRejectedContext(reason); _hooks.Value.Dispatch(ref rejected);
    }
    private void StopRound()
    {
        _round = null; _timer?.Cancel(); _timer = null; _lastStatus = "waiting_for_round";
        foreach (var box in _boxes.ToArray()) box.Dispose();
        _boxes.Clear();
    }
}
