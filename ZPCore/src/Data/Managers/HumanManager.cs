using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZPCore.Config.Core;
using ZPCore.Config.Round;
using ZPCore.Data.Extensions;
using ZPCore.Data.Humans;
using ZPCore.Data.Lifecycle;
using IEventSubscriber = ZPApi.Events.IEventSubscriber;

namespace ZPCore.Data.Managers;

internal class HumanManager(ISwiftlyCore core, IEventSubscriber eventSubscriber, IOptions<ZombiePlagueCoreConfig> config) : ILifecycle
{
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
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
    }
    
    public void Dispose()
    {
        core.GameEvent.Unhook(_onRoundStartEvent);
        core.GameEvent.Unhook(_onPlayerDisconnectEvent);
        core.GameEvent.Unhook(_onPlayerDeathEvent);
        eventSubscriber.OnPlayerInfected -= OnPlayerInfected;
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
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
        return _humans.GetValueOrDefault(player);
    }

    public void Respawn(IPlayer player)
    {
        if (player.IsAlive)
        {
            return;
        }
        
        if (player.IsInfected())
        {
            return;
        }

        if (!player.IsHuman())
        {
            var human = new Human(player);
            _humans.Add(player, human);
        }
        
        player.SwitchTeam(Team.CT);
        player.Respawn();
        ApplyPlayerHumanModel(player);
    }

    public int GetHumanCount()
    {
        return _humans.Count;
    }

    public void SetSurvivor(IPlayer player, ISurvivorConfig roundSettings)
    {
        var playerPawn = player.PlayerPawn;
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
        player.SetModel(roundSettings.SurvivorModel);

        var itemServices = playerPawn.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_negev");
        itemServices.GiveItem("weapon_knife");
    }
    
    public void ApplyPlayerHumanModel(IPlayer player)
    {
        var players = core.PlayerManager.GetAlive();
        player.SetModel(config.Value.DefaultHumanModel);
    }

    public bool IsSurvivor(IPlayer player)
    {
        if (_humans.TryGetValue(player, out var human))
        {
            return human.IsSurvivor;
        }

        return false;
    }
    
    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var pawn = @event.WeaponServices.Pawn;
        var player = core.PlayerManager.GetPlayerFromPawn(pawn);

        if (player == null || !player.IsValid)
        {
            return;
        }

        if (player.IsSurvivor())
        {
            @event.Result = HookResult.Stop;
        }
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        ClearAndAddHumans();
        ApplyAllPlayerHumanModel();
        
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

    private void ApplyAllPlayerHumanModel()
    {
        var alivePlayers = core.PlayerManager.GetAlive();
        foreach (var player in alivePlayers)
        {
            ApplyPlayerHumanModel(player);
        }
    }
}