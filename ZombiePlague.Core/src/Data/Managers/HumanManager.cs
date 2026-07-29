using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Humans;
using ZombiePlague.Core.Utils.Extensions;
using IEventSubscriber = ZombiePlague.Api.Events.IEventSubscriber;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class HumanManager(ISwiftlyCore core, IEventSubscriber eventSubscriber) : IHumanManager
{
    private readonly Dictionary<int, Human> _humans = [];
    
    private Guid _onRoundStartEvent;
    private Guid _onPlayerDisconnectEvent;
    private Guid _onPlayerDeathEvent;
    private bool _hooksRegistered;
    
    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _onRoundStartEvent = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        _onPlayerDisconnectEvent = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        _onPlayerDeathEvent = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        eventSubscriber.OnPlayerInfected += OnPlayerInfected;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        _hooksRegistered = true;
    }
    
    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        core.GameEvent.Unhook(_onRoundStartEvent);
        core.GameEvent.Unhook(_onPlayerDisconnectEvent);
        core.GameEvent.Unhook(_onPlayerDeathEvent);
        eventSubscriber.OnPlayerInfected -= OnPlayerInfected;
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
        _humans.Clear();
        _hooksRegistered = false;
    }

    private Human? GetHuman(IPlayer player)
    {
        return _humans.GetValueOrDefault(player.PlayerID);
    }

    public void Respawn(IPlayer player)
    {
        if (player.IsAlive)
        {
            return;
        }
        
        if (!IsHuman(player))
        {
            _humans[player.PlayerID] = new Human(player);
        }
        
        player.SwitchTeam(Team.CT);
        player.Respawn();
        ApplyPlayerHumanModel(player);
    }

    public int GetHumanCount()
    {
        return _humans.Count;
    }

    public bool IsHuman(IPlayer player)
    {
        return _humans.ContainsKey(player.PlayerID);
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
            human = new Human(player);
            _humans[player.PlayerID] = human;
        }
        
        human.IsSurvivor = true;
        
        var zombieCount = core.PlayerManager.GetAlive().Count(alivePlayer => !IsHuman(alivePlayer));
        player.SetHealth(playerPawn.Health + (roundSettings.SurvivorBonusHealthPerZombie * zombieCount));
        player.SetModel(core, roundSettings.SurvivorModel);

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
        // player.SetModel(config.Value.DefaultHumanModel);
    }

    public bool IsSurvivor(IPlayer player)
    {
        if (_humans.TryGetValue(player.PlayerID, out var human))
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

        if (IsSurvivor(player))
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
        ScheduleInvalidHumanCleanup();
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        _humans.Remove(@event.PlayerID);
        
        return HookResult.Continue;
    }

    private void OnPlayerInfected(IPlayer player)
    {
        _humans.Remove(player.PlayerID);
    }

    private void ScheduleInvalidHumanCleanup()
    {
        core.Scheduler.NextTick(() =>
        {
            foreach (var (playerId, human) in _humans.ToArray())
            {
                var player = human.Player;
                if (!player.IsValid || !player.IsAlive || player.Controller.Team != Team.CT)
                {
                    _humans.Remove(playerId);
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
            if (player.IsValid)
            {
                _humans[player.PlayerID] = new Human(player);
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
