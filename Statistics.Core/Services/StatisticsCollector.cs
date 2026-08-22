using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common.Hooks;
using Statistics.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace Statistics.Core.Services;

internal sealed class StatisticsCollector(
    ISwiftlyCore core,
    PlayerStatisticsService playerStatisticsService
)
{
    private const int InfectionDeathWindowSeconds = 5;

    private readonly Dictionary<ulong, RoundParticipantState> _roundParticipants = [];

    private readonly Dictionary<ulong, long> _infectionTransitions = [];

    private IZombiePlagueApi? _zombiePlagueApi;

    private bool _isStarted;

    private bool _isRoundActive;

    private Guid _playerHurtHook = Guid.Empty;
    private Guid _playerDeathHook = Guid.Empty;
    private Guid _playerConnectHook = Guid.Empty;
    private Guid _playerDisconnectHook = Guid.Empty;
    private Guid _playerSpawnHook = Guid.Empty;
    private Guid _playerTeamHook = Guid.Empty;
    private Guid _roundEndHook = Guid.Empty;
    private Guid _gameRestartHook = Guid.Empty;

    public void Initialize(IZombiePlagueApi zombiePlagueApi)
    {
        ArgumentNullException.ThrowIfNull(zombiePlagueApi);

        if (_zombiePlagueApi is not null)
        {
            throw new InvalidOperationException("Zombie Plague API is already initialized!");
        }

        _zombiePlagueApi = zombiePlagueApi;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        var zombiePlagueApi = GetZombiePlagueApi();

        _playerHurtHook = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurt);
        _playerDeathHook = core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);
        _playerConnectHook = core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnect);
        _playerDisconnectHook = core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _playerSpawnHook = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        _playerTeamHook = core.GameEvent.HookPost<EventPlayerTeam>(OnPlayerTeam);
        _roundEndHook = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _gameRestartHook = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);

        zombiePlagueApi.Events.Pre.PlayerInfect.Hook(OnPlayerInfecting, HookPriority.Low);
        zombiePlagueApi.Events.Post.PlayerInfectEvent += OnPlayerInfected;
        zombiePlagueApi.Events.Post.RoundStartEvent += OnRoundStarted;

        _isStarted = true;
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        var zombiePlagueApi = GetZombiePlagueApi();

        zombiePlagueApi.Events.Pre.PlayerInfect.Unhook(OnPlayerInfecting);
        zombiePlagueApi.Events.Post.PlayerInfectEvent -= OnPlayerInfected;
        zombiePlagueApi.Events.Post.RoundStartEvent -= OnRoundStarted;

        core.GameEvent.Unhook(_playerHurtHook);
        core.GameEvent.Unhook(_playerDeathHook);
        core.GameEvent.Unhook(_playerConnectHook);
        core.GameEvent.Unhook(_playerDisconnectHook);
        core.GameEvent.Unhook(_playerSpawnHook);
        core.GameEvent.Unhook(_playerTeamHook);
        core.GameEvent.Unhook(_roundEndHook);
        core.GameEvent.Unhook(_gameRestartHook);

        _roundParticipants.Clear();
        _infectionTransitions.Clear();
        _isRoundActive = false;
        _isStarted = false;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        if (!_isRoundActive || @event.ActualDmgHealth <= 0)
        {
            return HookResult.Continue;
        }

        var attacker = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (!CanTrack(attacker) ||
            !CanTrack(victim) ||
            attacker.SteamID == victim.SteamID)
        {
            return HookResult.Continue;
        }

        var attackerRole = GetRole(attacker);
        var victimRole = GetRole(victim);

        SetRole(attacker, attackerRole, participated: true);
        SetRole(victim, victimRole, participated: true);

        switch (attackerRole, victimRole)
        {
            case (PlayerRole.Human, PlayerRole.Zombie):
                playerStatisticsService.RecordDamageToZombies(
                    attacker.SteamID,
                    @event.ActualDmgHealth
                );
                break;

            case (PlayerRole.Zombie, PlayerRole.Human):
                playerStatisticsService.RecordDamageToHumans(
                    attacker.SteamID,
                    @event.ActualDmgHealth
                );
                break;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        if (!_isRoundActive)
        {
            return HookResult.Continue;
        }

        var victim = @event.UserIdPlayer;

        if (!CanTrack(victim))
        {
            return HookResult.Continue;
        }

        if (ShouldSuppressInfectionDeath(@event, victim))
        {
            return HookResult.Continue;
        }

        var victimRole = GetTrackedRole(victim);

        SetRole(victim, victimRole, participated: true);

        var attacker = @event.AttackerPlayer;

        if (CanTrack(attacker) && attacker.SteamID != victim.SteamID)
        {
            var attackerRole = GetRole(attacker);

            SetRole(attacker, attackerRole, participated: true);

            if (attackerRole == PlayerRole.Human && victimRole == PlayerRole.Zombie)
            {
                playerStatisticsService.RecordZombieKill(attacker.SteamID, @event.Headshot);
            }
        }

        playerStatisticsService.RecordDeath(victim.SteamID, victimRole);

        DetectLastHuman(
            victimRole == PlayerRole.Human ? victim.SteamID : null
        );

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnect(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (CanTrack(player))
        {
            playerStatisticsService.Initialize(player);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null || player.IsFakeClient || player.SteamID == 0)
        {
            return HookResult.Continue;
        }

        var keepSession = _isRoundActive && _roundParticipants.ContainsKey(player.SteamID);

        if (keepSession &&
            _roundParticipants[player.SteamID].CurrentRole == PlayerRole.Human)
        {
            DetectLastHuman(player.SteamID);
        }

        playerStatisticsService.Disconnect(player, keepSession);

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        TrackChangedRole(@event.UserIdPlayer);

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        if (!@event.Disconnect)
        {
            TrackChangedRole(@event.UserIdPlayer);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        if (!_isRoundActive)
        {
            return HookResult.Continue;
        }

        var winner = (Team)@event.Winner;

        foreach (var (steamId, participant) in _roundParticipants.ToArray())
        {
            var player = core.PlayerManager.GetPlayerFromSteamId(steamId, allowUnauthorized: false);
            var isConnected = CanTrack(player);
            var isPlaying = isConnected && IsPlayingTeam(player!);

            if (isPlaying)
            {
                SetRole(player!, GetRole(player!), participated: player!.IsAlive);
            }

            var survivedRound =
                isConnected &&
                player!.IsAlive &&
                participant.CurrentRole == PlayerRole.Human;

            var humanWon =
                isPlaying &&
                winner == Team.CT &&
                participant.WasHuman &&
                participant.CurrentRole == PlayerRole.Human;

            var zombieWon =
                isPlaying &&
                winner == Team.T &&
                participant.WasZombie &&
                participant.CurrentRole == PlayerRole.Zombie;

            var result = new RoundStatisticsResult(
                WasHuman: participant.WasHuman,
                WasZombie: participant.WasZombie,
                WasFirstZombie: participant.WasFirstZombie,
                WasLastHuman: participant.WasLastHuman,
                SurvivedRound: survivedRound,
                HumanWon: humanWon,
                ZombieWon: zombieWon,
                LastHumanSurvived: participant.WasLastHuman && survivedRound && humanWon
            );

            playerStatisticsService.RecordRound(steamId, result);
        }

        FinishRound();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        AbortRound();

        return HookResult.Continue;
    }

    private void OnPlayerInfecting(ref PlayerInfectPreContext context)
    {
        if (!_isRoundActive || context.IsCancelled || !CanTrack(context.Player))
        {
            return;
        }

        var infector = context.Infector;

        if (!CanTrack(infector) || infector.SteamID == context.Player.SteamID)
        {
            _infectionTransitions.Remove(context.Player.SteamID);

            return;
        }

        _infectionTransitions[context.Player.SteamID] =
            Stopwatch.GetTimestamp() + Stopwatch.Frequency * InfectionDeathWindowSeconds;

        SetRole(context.Player, PlayerRole.Human, participated: true);
    }

    private void OnPlayerInfected(ref PlayerInfectPostContext context)
    {
        if (!_isRoundActive || !CanTrack(context.Player))
        {
            return;
        }

        var player = context.Player;
        var infector = context.Infector;

        if (CanTrack(infector) && infector.SteamID != player.SteamID)
        {
            SetRole(infector, PlayerRole.Zombie, participated: true);
            SetRole(player, PlayerRole.Human, participated: true);

            playerStatisticsService.RecordInfection(infector.SteamID, player.SteamID);
        }

        SetRole(player, PlayerRole.Zombie, participated: player.IsAlive);

        DetectLastHuman();
    }

    private void OnRoundStarted(ref RoundStartPostContext context)
    {
        _ = context.Round;

        _roundParticipants.Clear();
        _infectionTransitions.Clear();
        _isRoundActive = true;

        playerStatisticsService.ResetAllStreaks();

        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            if (!CanTrack(player) || !IsActivePlayer(player))
            {
                continue;
            }

            SetRole(
                player,
                GetRole(player),
                participated: true,
                isInitialRole: true
            );
        }

        DetectLastHuman();
    }

    private void TrackChangedRole(IPlayer? player)
    {
        if (!_isRoundActive || !CanTrack(player) || !IsActivePlayer(player))
        {
            return;
        }

        var role = GetRole(player);

        if (role == PlayerRole.Zombie || _roundParticipants.ContainsKey(player.SteamID))
        {
            SetRole(player, role, participated: true);
            DetectLastHuman();
        }
    }

    private void SetRole(
        IPlayer player,
        PlayerRole role,
        bool participated,
        bool isInitialRole = false
    )
    {
        if (!_roundParticipants.TryGetValue(player.SteamID, out var participant))
        {
            participant = new RoundParticipantState();
            _roundParticipants[player.SteamID] = participant;
        }

        if (participant.CurrentRole is not PlayerRole.None &&
            participant.CurrentRole != role)
        {
            playerStatisticsService.ResetStreaks(player.SteamID);
        }

        participant.CurrentRole = role;

        if (!participated)
        {
            return;
        }

        switch (role)
        {
            case PlayerRole.Human:
                participant.WasHuman = true;
                break;

            case PlayerRole.Zombie:
                participant.WasZombie = true;
                participant.WasFirstZombie |= isInitialRole;
                break;
        }
    }

    private void DetectLastHuman(ulong? excludedSteamId = null)
    {
        if (!_isRoundActive)
        {
            return;
        }

        var aliveHumans = core.PlayerManager
            .GetAllValidPlayers()
            .Where(player =>
                CanTrack(player) &&
                IsActivePlayer(player) &&
                player.SteamID != excludedSteamId &&
                GetRole(player) == PlayerRole.Human
            )
            .Take(2)
            .ToArray();

        if (aliveHumans.Length != 1)
        {
            return;
        }

        var lastHuman = aliveHumans[0];

        SetRole(lastHuman, PlayerRole.Human, participated: true);

        _roundParticipants[lastHuman.SteamID].WasLastHuman = true;
    }

    private bool ShouldSuppressInfectionDeath(
        EventPlayerDeath @event,
        IPlayer player
    )
    {
        if (!string.Equals(@event.Weapon, "biohazard", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_infectionTransitions.Remove(player.SteamID, out var expiresAt))
        {
            return false;
        }

        return expiresAt >= Stopwatch.GetTimestamp() &&
               GetRole(player) == PlayerRole.Zombie;
    }

    private PlayerRole GetRole(IPlayer player)
    {
        return GetZombiePlagueApi().IsInfected(player)
            ? PlayerRole.Zombie
            : PlayerRole.Human;
    }

    private PlayerRole GetTrackedRole(IPlayer player)
    {
        return _roundParticipants.TryGetValue(player.SteamID, out var participant) &&
               participant.CurrentRole is not PlayerRole.None
            ? participant.CurrentRole
            : GetRole(player);
    }

    private void FinishRound()
    {
        _roundParticipants.Clear();
        _infectionTransitions.Clear();
        _isRoundActive = false;

        playerStatisticsService.ResetAllStreaks();
        playerStatisticsService.RemoveDisconnectedSessions();
    }

    private void AbortRound()
    {
        if (!_isRoundActive)
        {
            return;
        }

        FinishRound();
    }

    private IZombiePlagueApi GetZombiePlagueApi()
    {
        return _zombiePlagueApi
               ?? throw new InvalidOperationException("Zombie Plague API is not initialized!");
    }

    private static bool CanTrack([NotNullWhen(true)] IPlayer? player)
    {
        return player is
        {
            IsValid: true,
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }

    private static bool IsActivePlayer(IPlayer player)
    {
        return player.IsAlive && IsPlayingTeam(player);
    }

    private static bool IsPlayingTeam(IPlayer player)
    {
        return player.Controller.Team is Team.T or Team.CT;
    }
}
