using CS2ZombiePlague.Config.SupplyBox;
using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.Rounds.Contracts;
using CS2ZombiePlague.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using IEventSubscriber = CS2ZombiePlague.Data.Events.IEventSubscriber;

namespace CS2ZombiePlague.Data.Plugins.SupplyBox;

public sealed class SupplyBox
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly SupplyBoxMapConfigService _mapConfigService = DependencyManager.GetService<SupplyBoxMapConfigService>();
    private readonly SupplyBoxMenuService _menuService = DependencyManager.GetService<SupplyBoxMenuService>();
    private readonly SupplyBoxEditService _editService = DependencyManager.GetService<SupplyBoxEditService>();
    private readonly IEventSubscriber _eventSubscriber = DependencyManager.GetService<IEventSubscriber>();
    private readonly IEventPublisher _eventPublisher = DependencyManager.GetService<IEventPublisher>();
    private readonly CommonUtils _commonUtils = DependencyManager.GetService<CommonUtils>();
    private readonly ISupplyBoxConfig _config = DependencyManager.GetService<IOptions<SupplyBoxConfig>>().Value;
    
    private readonly List<SupplyBoxEntity> _droppedSupplyBoxes = [];
    private CancellationTokenSource? _respawnSupplyBoxThinker;

    public void RegisterHooks()
    {
        _core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
        _core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);
        
        _eventSubscriber.OnGameRoundStarted += OnGameRoundStarted;
        _eventSubscriber.OnSupplyBoxPickedUp += OnSupplyBoxPickedUp;
        _core.Event.OnMapLoad += OnMapLoad;
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
    
    private void OnSupplyBoxPickedUp(IPlayer player, SupplyBoxEntity supplyBox)
    {
        _droppedSupplyBoxes.Remove(supplyBox);
    }
    
    private void OnGameRoundStarted(IRound round)
    {
        CreateRespawnTimer(round);
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

    private bool IsDropSuccessful()
    {
        return _commonUtils.RandomNum(0, 100) <= _config.ChanceDrop;
        
    }
    
    private bool CanDrop(IRound round)
    {
        if (round is None || round is Armageddon || round is Survivor || round is Nemesis)
        {
            return false;
        }
        
        if (_droppedSupplyBoxes.Count >= _config.MaxCountTogether)
        {
            return false;
        }
        
        return true;
    }
    
    private void CreateRespawnTimer(IRound round)
    {
        var respawnTime = _commonUtils.RandomNum(_config.RespawnTimeBySeconds, _config.RespawnTimeBySeconds+_config.TimeSpreadBySeconds);
        _respawnSupplyBoxThinker = _core.Scheduler.DelayBySeconds(respawnTime, () =>
        {
            TrySpawnSupplyBox(round);
        });
    }
    
    private void OnMapLoad(IOnMapLoadEvent @event)
    { 
        _mapConfigService.LoadConfig(@event.MapName);
    }
    
    private HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        var player = @event.UserIdPlayer;
        if (player != null && @event.Text == "!supply" && player.IsAlive)
        {
            _menuService.ShowMainMenu(player);
        }
        
        return HookResult.Continue;
    }

    private void SpawnSupplyBox()
    {
        var supplyBox = _editService.TrySpawnUniqueSupplyBox(_droppedSupplyBoxes);
        
        if (supplyBox == null)
        {
            return;
        }
        
        _eventPublisher.OnSupplyBoxDropped(supplyBox);
        _droppedSupplyBoxes.Add(supplyBox);
    }
}