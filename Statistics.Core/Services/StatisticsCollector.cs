using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Common.Hooks;
using Microsoft.Extensions.Logging;
using Statistics.Core.Data;
using Statistics.Core.Points;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace Statistics.Core.Services;

internal sealed class StatisticsCollector(
    ISwiftlyCore core,
    PlayerStatisticsService playerStatisticsService,
    IRoundPointsFormulaProvider pointsFormulaProvider,
    PointsCalculator pointsCalculator
)
{
    private const int InfectionDeathWindowSeconds = 5;

    private readonly Dictionary<ulong, RoundParticipantState> _roundParticipants = [];

    private readonly Dictionary<ulong, long> _infectionTransitions = [];

    private readonly Dictionary<ulong, long> _pendingPointsNotifications = [];

    private IZombiePlagueApi? _zombiePlagueApi;

    private PointsFormula? _roundFormula;

    private PointsFormula? _mapFormula;

    private bool _isStarted;

    private bool _isRoundActive;

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

        _playerDeathHook = core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);
        _playerConnectHook = core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnect);
        _playerDisconnectHook = core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _playerSpawnHook = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        _playerTeamHook = core.GameEvent.HookPost<EventPlayerTeam>(OnPlayerTeam);
        _roundEndHook = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _gameRestartHook = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);

        zombiePlagueApi.Events.Players.Infecting.Hook(OnPlayerInfecting, HookPriority.Low);
        zombiePlagueApi.Events.Players.Infected.Hook(OnPlayerInfected);
        zombiePlagueApi.Events.Rounds.Started.Hook(OnRoundStarted);

        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;

        CaptureMapFormula("server startup");
        _isStarted = true;
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        var zombiePlagueApi = GetZombiePlagueApi();

        zombiePlagueApi.Events.Players.Infecting.Unhook(OnPlayerInfecting);
        zombiePlagueApi.Events.Players.Infected.Unhook(OnPlayerInfected);
        zombiePlagueApi.Events.Rounds.Started.Unhook(OnRoundStarted);

        core.Event.OnMapLoad -= OnMapLoad;
        core.Event.OnMapUnload -= OnMapUnload;

        core.GameEvent.Unhook(_playerDeathHook);
        core.GameEvent.Unhook(_playerConnectHook);
        core.GameEvent.Unhook(_playerDisconnectHook);
        core.GameEvent.Unhook(_playerSpawnHook);
        core.GameEvent.Unhook(_playerTeamHook);
        core.GameEvent.Unhook(_roundEndHook);
        core.GameEvent.Unhook(_gameRestartHook);

        _roundParticipants.Clear();
        _infectionTransitions.Clear();
        _pendingPointsNotifications.Clear();
        _roundFormula = null;
        _mapFormula = null;
        _isRoundActive = false;
        _isStarted = false;
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
        var victimParticipant = SetRole(victim, victimRole);
        var attacker = @event.AttackerPlayer;

        if (CanTrack(attacker) && attacker.SteamID != victim.SteamID)
        {
            var attackerRole = GetRole(attacker);
            var attackerParticipant = SetRole(attacker, attackerRole);

            if (attackerRole == PlayerRole.Human && victimRole == PlayerRole.Zombie)
            {
                var currentStreak = attackerParticipant.RecordZombieKill();

                playerStatisticsService.RecordZombieKill(
                    attacker.SteamID,
                    currentStreak
                );
            }
        }

        victimParticipant.RecordDeath();
        playerStatisticsService.RecordDeath(victim.SteamID);

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

        var keepSession = _isRoundActive &&
                          _roundParticipants.ContainsKey(player.SteamID);

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
        var formula = _roundFormula ?? _mapFormula ?? pointsFormulaProvider.CaptureFormula();

        foreach (var (steamId, participant) in _roundParticipants.ToArray())
        {
            var player = core.PlayerManager.GetPlayerFromSteamId(
                steamId,
                allowUnauthorized: false
            );

            var isConnected = CanTrack(player);
            var isPlaying = isConnected && IsPlayingTeam(player!);

            if (isPlaying)
            {
                participant.SetRole(GetRole(player!));
            }

            var humanWon =
                isPlaying &&
                player!.IsAlive &&
                winner == Team.CT &&
                participant.CurrentRole == PlayerRole.Human;

            var zombieWon =
                isPlaying &&
                player!.IsAlive &&
                winner == Team.T &&
                participant.CurrentRole == PlayerRole.Zombie;

            var pointsDelta = CalculatePoints(
                steamId,
                formula,
                participant.CreatePointsContext(humanWon, zombieWon)
            );

            var appliedPointsDelta = playerStatisticsService.RecordRound(
                steamId,
                new RoundStatisticsResult(
                    PointsDelta: pointsDelta,
                    HumanWon: humanWon,
                    ZombieWon: zombieWon
                )
            );

            _pendingPointsNotifications[steamId] = appliedPointsDelta;
        }

        FinishRound();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        _ = @event;

        AbortRound();

        return HookResult.Continue;
    }

    private void OnPlayerInfecting(ref PlayerInfectingContext context)
    {
        if (!_isRoundActive || context.IsCancelled || !CanTrack(context.Player))
        {
            return;
        }

        _infectionTransitions[context.Player.SteamID] =
            Stopwatch.GetTimestamp() + Stopwatch.Frequency * InfectionDeathWindowSeconds;

        SetRole(context.Player, PlayerRole.Human);
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        if (!_isRoundActive || !CanTrack(context.Player))
        {
            return;
        }

        var player = context.Player;
        var infectedParticipant = SetRole(player, PlayerRole.Human);
        var infector = context.Infector;

        if (CanTrack(infector) && infector.SteamID != player.SteamID)
        {
            var infectorParticipant = SetRole(infector, PlayerRole.Zombie);
            var currentInfectionStreak = infectorParticipant.RecordInfectionMade();

            infectedParticipant.RecordTimesInfected();

            playerStatisticsService.RecordInfection(
                infector.SteamID,
                player.SteamID,
                currentInfectionStreak
            );
        }

        SetRole(player, PlayerRole.Zombie);
    }

    private void OnRoundStarted(ref RoundStartedContext context)
    {
        _ = context.Round;

        _roundParticipants.Clear();
        _infectionTransitions.Clear();
        _roundFormula = _mapFormula ?? pointsFormulaProvider.CaptureFormula();
        _isRoundActive = true;

        NotifyPendingPointsChanges();

        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            if (!CanTrack(player) || !IsActivePlayer(player))
            {
                continue;
            }

            SetRole(player, GetRole(player));
        }
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _ = @event;

        AbortRound();
        _pendingPointsNotifications.Clear();
        pointsFormulaProvider.Refresh();
        CaptureMapFormula("map load");
    }

    private void OnMapUnload(IOnMapUnloadEvent @event)
    {
        _ = @event;

        AbortRound();
        _pendingPointsNotifications.Clear();
        _mapFormula = null;
        playerStatisticsService.CheckpointAndSaveAll();
    }

    private void NotifyPendingPointsChanges()
    {
        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            if (!CanTrack(player) ||
                !_pendingPointsNotifications.Remove(player.SteamID, out var pointsDelta))
            {
                continue;
            }

            var localizer = core.Translation.GetPlayerLocalizer(player);
            var translationKey = pointsDelta switch
            {
                > 0 => "Statistics.PointsGained",
                < 0 => "Statistics.PointsLost",
                _ => "Statistics.PointsUnchanged"
            };
            var points = Math.Abs(pointsDelta).ToString(CultureInfo.InvariantCulture);
            var message = localizer[translationKey].Replace("{points}", points);
            var color = pointsDelta switch
            {
                > 0 => "green",
                < 0 => "red",
                _ => "grey"
            };

            player.SendChat($"[green][Statistics] [{color}]{message}");
        }
    }

    private void CaptureMapFormula(string lifecycle)
    {
        _mapFormula = pointsFormulaProvider.CaptureFormula();

        core.Logger.LogInformation(
            "Statistics points formula selected for {Lifecycle}: {PointsFormula}",
            lifecycle,
            _mapFormula.Source
        );
    }

    private void TrackChangedRole(IPlayer? player)
    {
        if (!_isRoundActive || !CanTrack(player) || !IsActivePlayer(player))
        {
            return;
        }

        SetRole(player, GetRole(player));
    }

    private RoundParticipantState SetRole(IPlayer player, PlayerRole role)
    {
        if (!_roundParticipants.TryGetValue(player.SteamID, out var participant))
        {
            participant = new RoundParticipantState();
            _roundParticipants[player.SteamID] = participant;
        }

        participant.SetRole(role);

        return participant;
    }

    private long CalculatePoints(
        ulong steamId,
        PointsFormula formula,
        RoundPointsContext context
    )
    {
        try
        {
            return pointsCalculator.CalculateDelta(formula, context);
        }
        catch (PointsFormulaException exception)
        {
            core.Logger.LogError(
                exception,
                "Failed to calculate round points for player {SteamId}. No points will be awarded for this round.",
                steamId
            );

            return 0;
        }
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
        _roundFormula = null;
        _isRoundActive = false;

        playerStatisticsService.SaveRound();
    }

    private void AbortRound()
    {
        if (_isRoundActive)
        {
            FinishRound();
        }
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
