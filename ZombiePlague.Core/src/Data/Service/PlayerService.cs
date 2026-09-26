using Common.Di.Diagnostics;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Coordinators;
using ZombiePlague.Core.Data.Coordinators.Contracts;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IPlayerService : IService;

internal sealed class PlayerService(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IRoundManager roundManager,
    IPlayerPreferencesCoordinator playerPreferencesCoordinator,
    ISpectatorAccess spectators
) : IPlayerService
{
    private readonly Dictionary<int, CancellationTokenSource> _playerReadyTimers = [];

    private Guid _playerConnectGuid = Guid.Empty;
    private Guid _playerSpawnGuid = Guid.Empty;
    private Guid _playerDeathGuid = Guid.Empty;
    private Guid _playerDisconnectGuid = Guid.Empty;
    private Guid _playerTeamPreGuid = Guid.Empty;
    private Guid _playerTeamGuid = Guid.Empty;

    private const int MaxPlayerReadyAttempts = 30;
    private const float PlayerReadyRetryDelaySeconds = 0.1f;

    public void Register()
    {
        _playerConnectGuid = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        _playerSpawnGuid = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        _playerDeathGuid = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        _playerDisconnectGuid = core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _playerTeamPreGuid = core.GameEvent.HookPre<EventPlayerTeam>(OnPlayerTeamPre);
        _playerTeamGuid = core.GameEvent.HookPost<EventPlayerTeam>(OnPlayerTeam);

        core.Event.OnClientPutInServer += OnClientPutInServer;
    }

    public void Unregister()
    {
        core.GameEvent.Unhook(_playerConnectGuid);
        core.GameEvent.Unhook(_playerSpawnGuid);
        core.GameEvent.Unhook(_playerDeathGuid);
        core.GameEvent.Unhook(_playerDisconnectGuid);
        core.GameEvent.Unhook(_playerTeamPreGuid);
        core.GameEvent.Unhook(_playerTeamGuid);

        core.Event.OnClientPutInServer -= OnClientPutInServer;

        foreach (var timer in _playerReadyTimers.Values)
        {
            timer.Cancel();
        }

        _playerReadyTimers.Clear();

        playerPreferencesCoordinator.SaveAllAndWait();
        playerManager.Clear();
    }

    private void OnClientPutInServer(IOnClientPutInServerEvent @event)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.client_put_in_server", @event.PlayerId);
        if (@event.Kind != ClientKind.Bot)
        {
            return;
        }

        var player = core.PlayerManager.GetPlayer(@event.PlayerId);
        timing?.Identify(player);

        if (player != null)
        {
            playerManager.TrySetHuman(player);
        }
    }

    // player_connect_full может прийти до появления валидного Pawn. В SwiftlyS2 IsValid
    // требует одновременно Controller и Pawn, поэтому одноразовая проверка теряла late-join игроков.
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.Player.player_connect_full");
        var player = @event.UserIdPlayer;
        timing?.Identify(player);

        if (player is null)
        {
            return HookResult.Continue;
        }

        InitializePlayerWhenReady(player.PlayerID, player.SessionId, attempt: 0);

        return HookResult.Continue;
    }

    private void InitializePlayerWhenReady(int playerId, ulong sessionId, int attempt)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.player_ready", playerId, sessionId, attempt);
        var player = core.PlayerManager.GetPlayer(playerId);

        if (player is null || player.SessionId != sessionId)
        {
            CancelPlayerReadyTimer(playerId);
            return;
        }

        timing?.Identify(player);
        if (player.IsValid)
        {
            CancelPlayerReadyTimer(playerId);

            using (ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.preferences", playerId, sessionId, attempt))
            {
                playerPreferencesCoordinator.Initialize(player);
            }

            // Игрок с правом наблюдателя, ушедший в наблюдатели, остаётся там после смены карты
            // или переподключения, а не входит в CT автоматически.
            if (spectators.ChoseSpectators(player))
            {
                player.ChangeTeam(Team.Spectator);
                return;
            }

            bool humanized;
            using (ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.humanize", playerId, sessionId, attempt))
            {
                humanized = playerManager.TrySetHuman(player);
            }

            if (!humanized)
            {
                return;
            }

            // Если игрок подключился во время preparation или активного Infection/Plague
            // уже мёртвым, сразу передаём его текущему round lifecycle.
            if (!player.IsAlive)
            {
                using var respawnTiming = ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.respawn", playerId, sessionId, attempt);
                roundManager.TryRespawnPlayer(player);
            }

            return;
        }

        if (attempt >= MaxPlayerReadyAttempts)
        {
            CancelPlayerReadyTimer(playerId);
            return;
        }

        CancelPlayerReadyTimer(playerId);

        CancellationTokenSource? timer = null;
        timer = core.Scheduler.DelayBySeconds(PlayerReadyRetryDelaySeconds, () =>
        {
            if (!_playerReadyTimers.TryGetValue(playerId, out var currentTimer) ||
                !ReferenceEquals(currentTimer, timer))
            {
                return;
            }

            _playerReadyTimers.Remove(playerId);
            InitializePlayerWhenReady(playerId, sessionId, attempt + 1);
        });

        _playerReadyTimers[playerId] = timer;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "ZombiePlague.player_spawn");
        var player = @event.UserIdPlayer;
        timing?.Identify(player);

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
        CancelPlayerReadyTimer(@event.PlayerID);

        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        playerPreferencesCoordinator.SaveAndRemove(player);
        playerManager.Remove(player);

        return HookResult.Continue;
    }

    private static HookResult OnPlayerTeamPre(EventPlayerTeam @event)
    {
        // Скрываем сообщение до рассылки события; игровая логика выполняется в Post-хуке.
        @event.Silent = true;

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        return roundManager.OnPlayerTeam(@event);
    }

    private void CancelPlayerReadyTimer(int playerId)
    {
        if (_playerReadyTimers.Remove(playerId, out var timer))
        {
            timer.Cancel();
        }
    }
}
