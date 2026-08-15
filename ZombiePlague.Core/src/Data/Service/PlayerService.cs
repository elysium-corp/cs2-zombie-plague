using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Coordinators;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IPlayerService : IService;

internal sealed class PlayerService(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IRoundManager roundManager,
    PlayerPreferencesCoordinator playerPreferencesCoordinator
) : IPlayerService
{
    private Guid _playerConnectGuid = Guid.Empty;
    private Guid _playerSpawnGuid = Guid.Empty;
    private Guid _playerDeathGuid = Guid.Empty;
    private Guid _playerDisconnectGuid = Guid.Empty;
    private Guid _playerTeamGuid = Guid.Empty;

    public void Register()
    {
        _playerConnectGuid = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        _playerSpawnGuid = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        _playerDeathGuid = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        _playerDisconnectGuid = core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _playerTeamGuid = core.GameEvent.HookPost<EventPlayerTeam>(OnPlayerTeam);

        core.Event.OnClientPutInServer += OnClientPutInServer;
    }

    public void Unregister()
    {
        core.GameEvent.Unhook(_playerConnectGuid);
        core.GameEvent.Unhook(_playerSpawnGuid);
        core.GameEvent.Unhook(_playerDeathGuid);
        core.GameEvent.Unhook(_playerDisconnectGuid);
        core.GameEvent.Unhook(_playerTeamGuid);

        core.Event.OnClientPutInServer -= OnClientPutInServer;

        playerPreferencesCoordinator.SaveAllAndWait();
        playerManager.Clear();
    }

    private void OnClientPutInServer(IOnClientPutInServerEvent @event)
    {
        if (@event.Kind != ClientKind.Bot)
        {
            return;
        }

        var player = core.PlayerManager.GetPlayer(@event.PlayerId);

        if (player != null)
        {
            playerManager.TrySetHuman(player);
        }
    }

    // Pre используется специально: следующие Post-хуки увидят уже созданную роль игрока.
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player?.IsValid != true)
        {
            return HookResult.Continue;
        }

        playerPreferencesCoordinator.Initialize(player);
        playerManager.TrySetHuman(player);

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;

        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        playerManager.TryApplyRole(player);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        // Позволяем другим обработчикам смерти завершить работу до снятия роли.
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (player.IsValid)
            {
                playerManager.TryDeactivateRole(player);
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        playerPreferencesCoordinator.SaveAndRemove(player);
        playerManager.Remove(player);

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        return roundManager.OnPlayerTeam(@event);
    }
}
