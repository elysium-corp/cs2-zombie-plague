using Common.Di;
using Common.Di.Utils;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SupplyBox.Api;
using SupplyBox.Api.Events;
using SupplyBox.Api.Events.Contexts;
using SupplyBox.Data;
using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;
using SupplyBox.Di;
using SupplyBox.Services;
using SupplyBox.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Api;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Api.Events.Contexts.Round;

namespace SupplyBox;

[PluginMetadata(
    Id = "SupplyBox.Core",
    Version = "0.1.0",
    Name = "[ZP] SupplyBox",
    Author = "illusion & fdrinv",
    Description = "Adds supply boxes that fall from the sky, can be picked up, and grant rewards")
]
internal sealed partial class SupplyBox(ISwiftlyCore core) : Plugin<SupplyBoxModule>(core)
{
    private readonly Lazy<SupplyBoxMapConfigService> _mapConfigService = GetRequiredServiceLazy<SupplyBoxMapConfigService>();
    private readonly Lazy<SupplyBoxMenuService> _menuService = GetRequiredServiceLazy<SupplyBoxMenuService>();
    private readonly Lazy<SupplyBoxEditService> _editService = GetRequiredServiceLazy<SupplyBoxEditService>();
    private readonly Lazy<IOptions<SupplyBoxConfig>> _config = GetRequiredServiceLazy<IOptions<SupplyBoxConfig>>();
    private readonly Lazy<IHookPublisher> _hooks = GetRequiredServiceLazy<IHookPublisher>();
    private readonly Lazy<ISupplyBoxEvents> _events = GetRequiredServiceLazy<ISupplyBoxEvents>();

    private Guid _guidOnEventRoundEndPost = Guid.Empty;
    private Guid _guidOnEventCsPreRestartPost = Guid.Empty;

    private readonly List<SupplyBoxEntity> _droppedSupplyBoxes = [];
    private CancellationTokenSource? _respawnSupplyBoxThinker;

    public static IZombiePlagueApi ZombiePlagueApi = null!;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var supplyBoxApi = new SupplyBoxApi(_events.Value);
        interfaceManager.AddSharedInterface<ISupplyBoxApi, SupplyBoxApi>(ISupplyBoxApi.SharedApiKey, supplyBoxApi);
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        ZombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnReady()
    {
        _guidOnEventRoundEndPost = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _guidOnEventCsPreRestartPost = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);

        _events.Value.Collected.Hook(OnSupplyBoxCollected);
        ZombiePlagueApi.Events.Rounds.Started.Hook(OnRoundStarted);

        core.Event.OnMapLoad += OnMapLoad;

        Core.Command.RegisterCommand(
            commandName: "supply",
            handler: SupplyEditorHandler,
            registerRaw: true
        );
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnEventRoundEndPost);
        Core.GameEvent.Unhook(_guidOnEventCsPreRestartPost);

        ZombiePlagueApi.Events.Rounds.Started.Unhook(OnRoundStarted);
        _events.Value.Collected.Unhook(OnSupplyBoxCollected);

        core.Event.OnMapLoad -= OnMapLoad;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _respawnSupplyBoxThinker?.Cancel();
        _droppedSupplyBoxes.Clear();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        _respawnSupplyBoxThinker?.Cancel();
        _droppedSupplyBoxes.Clear();

        return HookResult.Continue;
    }

    private void SupplyEditorHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (player == null)
        {
            return;
        }

        _menuService.Value.ShowMainMenu(player);
    }

    private void OnSupplyBoxCollected(ref SupplyBoxCollectedContext context)
    {
        var supplyBoxIndex = context.SupplyBox.Index;
        var box = _droppedSupplyBoxes.Find(box => box.Index == supplyBoxIndex);

        if (box != null)
        {
            _droppedSupplyBoxes.Remove(box);
        }
    }

    private void OnRoundStarted(ref RoundStartedContext context)
    {
        CreateRespawnTimer(context.Round);
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _mapConfigService.Value.LoadConfig(@event.MapName);
    }

    private void TrySpawnSupplyBox(IRound round)
    {
        CreateRespawnTimer(round);

        var rejectionReason = GetSpawnRejectionReason(round);

        if (rejectionReason is not null)
        {
            DispatchSpawnRejected(rejectionReason.Value);
            return;
        }

        if (!IsDropSuccessful())
        {
            DispatchSpawnRejected(SupplyBoxSpawnRejectionReason.ChanceMissed);
            return;
        }

        SpawnSupplyBox();
    }

    private void CreateRespawnTimer(IRound round)
    {
        var respawnTime = Numeric.Random(
            _config.Get().RespawnTimeBySeconds,
            _config.Get().RespawnTimeBySeconds + _config.Get().TimeSpreadBySeconds);

        _respawnSupplyBoxThinker = core.Scheduler.DelayBySeconds(respawnTime, () =>
        {
            TrySpawnSupplyBox(round);
        });
    }

    private void SpawnSupplyBox()
    {
        var preContext = new SupplyBoxSpawningContext(
            _droppedSupplyBoxes.Cast<ISupplyBoxEntity>().ToArray());

        if (!_hooks.Value.DispatchCancellable(ref preContext))
        {
            DispatchSpawnRejected(SupplyBoxSpawnRejectionReason.Cancelled);
            return;
        }

        var supplyBox = _editService.Value.TrySpawnUniqueSupplyBox(_droppedSupplyBoxes);

        if (supplyBox == null)
        {
            DispatchSpawnRejected(SupplyBoxSpawnRejectionReason.SpawnPointUnavailable);
            return;
        }

        _droppedSupplyBoxes.Add(supplyBox);

        var postContext = new SupplyBoxSpawnedContext(supplyBox);
        _hooks.Value.Dispatch(ref postContext);
    }

    private bool IsDropSuccessful()
    {
        return Numeric.Random(0, 100) <= _config.Get().ChanceDrop;
    }

    private SupplyBoxSpawnRejectionReason? GetSpawnRejectionReason(IRound round)
    {
        if (ZombiePlagueApi.IsSurvivorRound(round) || ZombiePlagueApi.IsNemesisRound(round))
        {
            return SupplyBoxSpawnRejectionReason.RoundNotSupported;
        }

        return _droppedSupplyBoxes.Count >= _config.Get().MaxCountTogether
            ? SupplyBoxSpawnRejectionReason.ActiveLimitReached
            : null;
    }

    private void DispatchSpawnRejected(SupplyBoxSpawnRejectionReason reason)
    {
        var context = new SupplyBoxSpawnRejectedContext(reason);
        _hooks.Value.Dispatch(ref context);
    }
}
