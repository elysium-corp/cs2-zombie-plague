using Common.Di;
using Common.Di.Utils;
using Microsoft.Extensions.Options;
using SupplyBox.Api;
using SupplyBox.Data;
using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;
using SupplyBox.Di;
using SupplyBox.Events;
using SupplyBox.Services;
using SupplyBox.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Round;
using IEventSubscriber = SupplyBox.Events.IEventSubscriber;

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
    private readonly Lazy<IEventPublisher> _eventPublisher = GetRequiredServiceLazy<IEventPublisher>();
    private readonly Lazy<IEventSubscriber> _eventSubscriber = GetRequiredServiceLazy<IEventSubscriber>();
    
    private Guid _guidOnEventRoundEndPost = Guid.Empty;
    private Guid _guidOnEventCsPreRestartPost = Guid.Empty;
    
    private readonly List<SupplyBoxEntity> _droppedSupplyBoxes = [];
    private CancellationTokenSource? _respawnSupplyBoxThinker;
    
    public static IZombiePlagueApi ZombiePlagueApi = null!;
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var supplyBoxApi = new SupplyBoxApi(_eventSubscriber.Value);
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
        
        _eventSubscriber.Value.OnSupplyBoxPickedUp += OnSupplyBoxPickedUp;
        ZombiePlagueApi.Events.Post.RoundStartEvent += OnRoundStarted;
        
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
        
        ZombiePlagueApi.Events.Post.RoundStartEvent -= OnRoundStarted;
        _eventSubscriber.Value.OnSupplyBoxPickedUp -= OnSupplyBoxPickedUp;
        
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
    
    private void OnSupplyBoxPickedUp(IPlayer player, ISupplyBoxEntity supplyBox)
    {
        var box = _droppedSupplyBoxes.Find(box => box.Index == supplyBox.Index);

        if (box == null)
        {
            return;
        }
        
        _droppedSupplyBoxes.Remove(box);
    }
    
    private void OnRoundStarted(ref RoundStartPostContext context)
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
        
        if (!CanDrop(round))
        {
            return;
        }
        
        if (!IsDropSuccessful())
        {
            return;
        }
        
        SpawnSupplyBox();
    }
    
    private void CreateRespawnTimer(IRound round)
    {
        var respawnTime = Numeric.Random(_config.Get().RespawnTimeBySeconds, _config.Get().RespawnTimeBySeconds+_config.Get().TimeSpreadBySeconds);

        _respawnSupplyBoxThinker = core.Scheduler.DelayBySeconds(respawnTime, () =>
        {
            TrySpawnSupplyBox(round);
        });
    }

    private void SpawnSupplyBox()
    {
        var supplyBox = _editService.Value.TrySpawnUniqueSupplyBox(_droppedSupplyBoxes);
        
        if (supplyBox == null)
        {
            return;
        }
        
        _eventPublisher.Value.OnSupplyBoxDropped(supplyBox);
        
        _droppedSupplyBoxes.Add(supplyBox);
    }
    
    private bool IsDropSuccessful()
    {
        return Numeric.Random(0, 100) <= _config.Get().ChanceDrop;
        
    }
    
    private bool CanDrop(IRound round)
    {
        if (ZombiePlagueApi.IsSurvivorRound(round) || ZombiePlagueApi.IsNemesisRound(round))
        {
            return false;
        }
        
        if (_droppedSupplyBoxes.Count >= _config.Get().MaxCountTogether)
        {
            return false;
        }
        
        return true;
    }
}