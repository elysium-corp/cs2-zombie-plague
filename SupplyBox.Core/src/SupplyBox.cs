using Common.Di;
using Common.Di.Utils;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Localization.Api;
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
    internal const string EditorPermission = "supply_box.admin.edit";
    private readonly Lazy<SupplyBoxMapConfigService> _mapConfigService = GetRequiredServiceLazy<SupplyBoxMapConfigService>();
    private readonly Lazy<SupplyBoxMenuService> _menuService = GetRequiredServiceLazy<SupplyBoxMenuService>();
    private readonly Lazy<SupplyBoxEditService> _editService = GetRequiredServiceLazy<SupplyBoxEditService>();
    private readonly Lazy<IOptions<SupplyBoxConfig>> _config = GetRequiredServiceLazy<IOptions<SupplyBoxConfig>>();
    private readonly Lazy<IHookPublisher> _hooks = GetRequiredServiceLazy<IHookPublisher>();
    private readonly Lazy<ISupplyBoxEvents> _events = GetRequiredServiceLazy<ISupplyBoxEvents>();

    private Guid _guidOnEventRoundEndPost = Guid.Empty;
    private Guid _guidOnEventCsPreRestartPost = Guid.Empty;
    private Guid _supplyCommand = Guid.Empty;

    private readonly List<SupplyBoxEntity> _droppedSupplyBoxes = [];
    private CancellationTokenSource? _respawnSupplyBoxThinker;
    private bool _roundActive;

    public static IZombiePlagueApi ZombiePlagueApi = null!;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var supplyBoxApi = new SupplyBoxApi(_events.Value);
        interfaceManager.AddSharedInterface<ISupplyBoxApi, SupplyBoxApi>(ISupplyBoxApi.SharedApiKey, supplyBoxApi);
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        ZombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnReady()
    {
        _guidOnEventRoundEndPost = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _guidOnEventCsPreRestartPost = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);

        _events.Value.Collected.Hook(OnSupplyBoxCollected);
        ZombiePlagueApi.Events.Rounds.Started.Hook(OnRoundStarted);

        core.Event.OnMapLoad += OnMapLoad;

        _supplyCommand = Core.Command.RegisterCommand(
            commandName: "supply",
            handler: SupplyEditorHandler,
            registerRaw: true,
            permission: EditorPermission
        );
    }

    protected override void OnUnload()
    {
        StopRound();
        Core.GameEvent.Unhook(_guidOnEventRoundEndPost);
        Core.GameEvent.Unhook(_guidOnEventCsPreRestartPost);

        ZombiePlagueApi.Events.Rounds.Started.Unhook(OnRoundStarted);
        _events.Value.Collected.Unhook(OnSupplyBoxCollected);

        core.Event.OnMapLoad -= OnMapLoad;

        if (_supplyCommand != Guid.Empty)
        {
            Core.Command.UnregisterCommand(_supplyCommand);
            _supplyCommand = Guid.Empty;
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        StopRound();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        StopRound();

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
        StopRound();
        _roundActive = true;
        CreateRespawnTimer(context.Round);
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _mapConfigService.Value.LoadConfig(@event.MapName);
    }

    private void TrySpawnSupplyBox(IRound round)
    {
        var rejectionReason = GetSpawnRejectionReason(round);

        if (rejectionReason is not null)
        {
            DispatchSpawnRejected(rejectionReason.Value);
            CreateRespawnTimer(round);
            return;
        }

        if (!IsDropSuccessful())
        {
            DispatchSpawnRejected(SupplyBoxSpawnRejectionReason.ChanceMissed);
            CreateRespawnTimer(round);
            return;
        }

        SpawnSupplyBox();
        CreateRespawnTimer(round);
    }

    private void CreateRespawnTimer(IRound round)
    {
        CancelRespawnTimer();
        if (!_roundActive) return;

        var minimum = Math.Max(1, _config.Get().RespawnTimeBySeconds);
        var spread = Math.Max(0, _config.Get().TimeSpreadBySeconds);
        var maximum = (int)Math.Min(int.MaxValue, (long)minimum + spread);
        var respawnTime = minimum == maximum ? minimum : Numeric.Random(minimum, maximum);

        CancellationTokenSource? timer = null;
        timer = core.Scheduler.DelayBySeconds(respawnTime, () =>
        {
            if (!_roundActive || !ReferenceEquals(_respawnSupplyBoxThinker, timer)) return;
            _respawnSupplyBoxThinker = null;
            TrySpawnSupplyBox(round);
        });
        _respawnSupplyBoxThinker = timer;
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
        return Numeric.Random(0, 100) < Math.Clamp(_config.Get().ChanceDrop, 0, 100);
    }

    private void StopRound()
    {
        _roundActive = false;
        CancelRespawnTimer();
        foreach (var box in _droppedSupplyBoxes.ToArray()) box.Dispose();
        _droppedSupplyBoxes.Clear();
    }

    private void CancelRespawnTimer()
    {
        _respawnSupplyBoxThinker?.Cancel();
        _respawnSupplyBoxThinker = null;
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
