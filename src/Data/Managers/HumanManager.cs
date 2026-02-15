using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Humans;
using CS2ZombiePlague.Data.Lifecycle;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class HumanManager(ISwiftlyCore core, IEventSubscriber eventSubscriber) : ILifecycle
{
    private readonly KnifeManager _knifeManager = DependencyManager.GetService<KnifeManager>();
    private readonly Dictionary<IPlayer, Human> _humans = new();
    
    private Guid _onRoundStartEvent;
    private Guid _onPlayerDisconnectEvent;
    private Guid _onPlayerDeathEvent;
    
    public void RegisterHooks()
    {
        _onRoundStartEvent = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        _onPlayerDisconnectEvent = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        _onPlayerDeathEvent = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        eventSubscriber.OnPlayerInfected += OnPlayerInfected;
    }
    
    public void Dispose()
    {
        core.GameEvent.Unhook(_onRoundStartEvent);
        core.GameEvent.Unhook(_onPlayerDisconnectEvent);
        core.GameEvent.Unhook(_onPlayerDeathEvent);
        eventSubscriber.OnPlayerInfected -= OnPlayerInfected;
    }

    public List<IPlayer> GetAllHumanPlayers()
    {
        return _humans.Keys.ToList();
    }
    
    public List<Human> GetAllHumans()
    {
        return _humans.Values.ToList();
    }

    public Human? GetHuman(IPlayer player)
    {
        return _humans.ContainsKey(player) ? _humans[player] : null;
    }

    public int GetHumanCount()
    {
        return _humans.Count;
    }

    public void SetSurvivor(IPlayer player, ISurvivorConfig roundSettings)
    {
        var playerPawn =  player.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }
        
        var human = GetHuman(player);
        if (human == null)
        {
            return;
        }
        
        human.IsSurvivor = true;
        
        var countPlayers = core.PlayerManager.GetAlive().Count();
        player.SetHealth(playerPawn.Health + (roundSettings.SurvivorBonusHealthPerZombie * countPlayers));
        player.SetModel(roundSettings.Model);

        var itemServices = playerPawn.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        _knifeManager.GiveKnife(player);
        itemServices.GiveItem("weapon_negev");
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        ClearAndAddHumans();
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        UpdateHumans();
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        UpdateHumans();
        
        return HookResult.Continue;
    }

    private void OnPlayerInfected(IPlayer player)
    {
        UpdateHumans();
    }

    private void UpdateHumans()
    {
        core.Scheduler.NextTick(() =>
        {
            var humansSnapshot = GetAllHumanPlayers();
            foreach (var human in humansSnapshot)
            {
                if (human.IsInfected() || !human.IsValid || !human.IsAlive)
                {
                    _humans.Remove(human);
                }
            }
        });
    }
    
    private void ClearAndAddHumans()
    {
        _humans.Clear();
        var allValidPlayer = core.PlayerManager.GetAllPlayers();
        foreach (var player in allValidPlayer)
        {
            if (!player.IsInfected())
            {
                var human = new Human(player);
                _humans.Add(player, human);
            }
        }
    }
}