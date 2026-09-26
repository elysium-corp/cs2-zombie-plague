using CustomHud.Api;
using Common.Hooks.Abstractions;
using Localization.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Api.Events.Contexts.Round;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Data.Service;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class RoundManager(
    ISwiftlyCore core,
    IOptions<ZombiePlagueCoreConfig> config,
    IPlayerManager playerManager,
    DamageMovementRestore damageMovementRestore,
    IRoundRegistrator roundRegistrator,
    IRoundFactory roundFactory,
    IHookPublisher hooks,
    Func<ILocalizationApi> localization,
    BannerNotificationClient? notifications = null
) : IRoundManager
{
    public RoundBase? CurrentRound { get; private set; }

    public RoundBase? NextRound { get; private set; }

    public bool IsPreparing => _preparationTimer is not null;

    private CancellationTokenSource? _preparationTimer;
    private readonly Dictionary<int, CancellationTokenSource> _preparationRespawns = [];

    private int _remainingPreparationTime;

    private bool _countdownSoundPlayed;
    private uint _countdownSoundEvent;
    private uint _preparationSoundEvent;

    // Смерть единственного ожидающего игрока уже завершила игровой раунд CS2;
    // до round_end подготовка не должна никого возрождать.
    private bool _nextRoundRequested;

    // Сколько тиков подряд в командах есть мёртвый игрок, которого не удалось возродить,
    // и сколько тиков ещё нельзя повторно перезапускать раунд ради них.
    private int _unspawnedTicks;
    private int _unspawnedRestartCooldown;

    private const float DelayPreparationTimer = 1.5f;

    private const int PeriodSecondsPreparationTask = 1;

    private const int MinimumPlayersFloor = 2;

    private const float WaitingRoundRestartDelay = 3.0f;

    private const int UnspawnedTicksBeforeRestart = 3;

    private const int UnspawnedRestartCooldownTicks = 120;

    private int RequiredPlayers => Math.Max(MinimumPlayersFloor, config.Value.MinimumPlayers);

    public void Prepare()
    {
        var preContext = new RoundPreparingContext();

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            return;
        }

        End();

        if (CurrentRound is not null)
        {
            return;
        }

        if (IsWarmupActive())
        {
            return;
        }

        _preparationSoundEvent =
            SoundExt.PlayGlobal(config.Value.PreparationSounds.GetRandomString(), config.Value.PreparationSoundVolume);

        var allPlayers = core.PlayerManager.GetAllPlayers();

        foreach (var player in allPlayers)
        {
            // Зритель сам решает, когда войти в игру: его не переводим в CT
            // и не учитываем в минимуме игроков. Роль прошлого раунда снимаем,
            // чтобы зритель не считался зомби; при входе в команду он получит новую.
            if (IsSpectator(player))
            {
                playerManager.Remove(player);
                continue;
            }

            playerManager.TrySetHuman(player);
        }

        _nextRoundRequested = false;

        StartPreparationTimer();

        var postContext = new RoundPreparedContext(_remainingPreparationTime);
        hooks.Dispatch(ref postContext);
    }

    public void Start()
    {
        if (_preparationTimer is null)
        {
            DispatchStartRejected(null, RoundStartRejectionReason.NotPreparing);
            return;
        }

        var round = TakeNextRound() ?? CreateRandomRound();

        if (round is null)
        {
            // Ни один режим не может начаться: подготовка продолжается,
            // единственного живого игрока нельзя делать первым зомби.
            DispatchStartRejected(null, RoundStartRejectionReason.CannotStart);
            return;
        }

        StartRound(round);
    }

    public RoundStartResult TryStartRound(RoundBase round)
    {
        if (_preparationTimer is null)
        {
            DispatchStartRejected(round.Id, RoundStartRejectionReason.NotPreparing);
            return RoundStartResult.NotPreparing;
        }

        if (!round.CanStart())
        {
            DispatchStartRejected(round.Id, RoundStartRejectionReason.CannotStart);
            return RoundStartResult.CannotStart;
        }

        return StartRound(round);
    }

    public RoundStartResult TryStartRandomRound()
    {
        if (!IsPreparing)
        {
            DispatchStartRejected(null, RoundStartRejectionReason.NotPreparing);
            return RoundStartResult.NotPreparing;
        }

        var round = CreateRandomRound();

        if (round is null)
        {
            DispatchStartRejected(null, RoundStartRejectionReason.CannotStart);
            return RoundStartResult.CannotStart;
        }

        return StartRound(round);
    }

    public void End()
    {
        var round = CurrentRound;

        if (round is null)
        {
            damageMovementRestore.Clear();
            StopPreparation();
            return;
        }

        var preContext = new RoundEndingContext(round);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            return;
        }

        damageMovementRestore.Clear();
        StopPreparation();

        try
        {
            round.End();
        }
        finally
        {
            CurrentRound = null;
        }

        var postContext = new RoundEndedContext(round);
        hooks.Dispatch(ref postContext);
    }

    public void ForceStop(bool dispatchEndedEvent = false)
    {
        damageMovementRestore.Clear();
        var round = CurrentRound;
        CurrentRound = null;
        StopPreparation();
        if (round is null) return;

        round.End();
        if (dispatchEndedEvent)
        {
            var context = new RoundEndedContext(round);
            hooks.Dispatch(ref context);
        }
    }

    public void SelectNextRound(RoundBase round)
    {
        var preContext = new RoundSchedulingContext(round);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            return;
        }

        NextRound = round;

        var postContext = new RoundScheduledContext(round);
        hooks.Dispatch(ref postContext);
    }

    public void ClearNextRound()
    {
        var round = NextRound;

        if (round is null)
        {
            return;
        }

        var preContext = new RoundScheduleClearingContext(round);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            return;
        }

        NextRound = null;

        var postContext = new RoundScheduleClearedContext(round);
        hooks.Dispatch(ref postContext);
    }

    public HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        return CurrentRound?.HandlePlayerConnectedFull(@event) ?? HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        if (IsPreparing)
        {
            // Пока игроков меньше минимума, смерть ожидающего игрока начинает следующий раунд:
            // CS2 заново возродит всех, а подготовка продолжит ждать второго игрока.
            if (IsWaitingForPlayers())
            {
                RequestNextRound();
            }
            else
            {
                ScheduleRespawn(@event.UserIdPlayer);
            }

            return HookResult.Continue;
        }

        return CurrentRound?.HandlePlayerDeath(@event) ?? HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        return CurrentRound?.HandlePlayerDisconnect(@event) ?? HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        if (IsPreparing)
        {
            // Без ботов вход в команду — единственный момент, когда игрок получает Pawn.
            // Возрождаем его человеком сразу, не дожидаясь очередного тика подготовки.
            if (!@event.Disconnect &&
                @event.Team is (byte)Team.T or (byte)Team.CT &&
                @event.OldTeam is not ((byte)Team.T or (byte)Team.CT) &&
                @event.UserIdPlayer is { } player)
            {
                core.Scheduler.NextWorldUpdate(() => AdmitPreparationPlayer(player));
            }

            return HookResult.Continue;
        }

        return CurrentRound?.HandlePlayerTeam(@event) ?? HookResult.Continue;
    }

    public void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();
        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();

        damageMovementRestore.Schedule(
            victim,
            afterPlayerDamage: attacker is { IsValid: true }
        );

        if (victim is { IsValid: true } &&
            playerManager.IsZombie(victim) &&
            (context.Params.Info.DamageType & DamageTypes_t.DMG_FALL) != 0)
        {
            context.Params.Info.Damage = 0;
            context.SetHookResult(HookResult.CancelOriginal);
            return;
        }

        CurrentRound?.HandleTakeDamage(ref context);
    }

    private void OnPrepareTask()
    {
        if (_preparationTimer is null || _nextRoundRequested)
        {
            return;
        }

        RespawnIdleParticipants();

        if (RestartForUnspawnedParticipants())
        {
            return;
        }

        // Пока в командах меньше минимума игроков, подготовка не расходует отсчёт:
        // раунд с одним игроком сразу закончился бы его заражением. IsPreparing
        // сохраняется, чтобы подключившийся игрок возродился человеком.
        if (IsWaitingForPlayers())
        {
            HoldCountdown();
            return;
        }

        if (_remainingPreparationTime > 0)
        {
            _remainingPreparationTime--;
        }

        if (!_countdownSoundPlayed && _remainingPreparationTime == 10)
        {
            PlayCountdownSound();
        }

        if (_remainingPreparationTime < 1)
        {
            // Погибший во время отсчёта игрок ещё ждёт возрождения: раунд начнётся,
            // когда живых участников снова хватит.
            if (CountParticipants(aliveOnly: true) >= RequiredPlayers)
            {
                Start();
            }

            return;
        }

        foreach (var player in core.PlayerManager.GetAllPlayers()
                     .Where(value => value is { IsAuthorized: true, IsFakeClient: false }))
        {
            notifications?.Publish(player, "ZombiePlague.Round.Preparing",
                new Dictionary<string, object?> { ["seconds"] = _remainingPreparationTime });
        }
    }

    private RoundBase? TakeNextRound()
    {
        if (NextRound is null)
        {
            return null;
        }

        return NextRound.CanStart() ? NextRound : null;
    }

    private RoundBase? CreateRandomRound()
    {
        var candidates = roundRegistrator
            .GetAllEnabled()
            .ToList();

        while (candidates.Count > 0)
        {
            var selectedConfig = SelectByWeight(candidates, Random.Shared);

            candidates.Remove(selectedConfig);

            var selectedRound = roundFactory.Create(selectedConfig);

            if (selectedRound.CanStart())
            {
                return selectedRound;
            }
        }

        // Инфекция остаётся резервным режимом, но только когда ей есть кого заражать.
        var infection = roundFactory.Create<Infection>();

        return infection.CanStart() ? infection : null;
    }

    internal static IRoundConfig SelectByWeight(IReadOnlyCollection<IRoundConfig> candidates, Random random)
    {
        if (candidates.Count == 0) throw new ArgumentException("At least one round is required.", nameof(candidates));
        var totalWeight = candidates.Sum(static round => Math.Max(0L, round.Weight));
        if (totalWeight <= 0) return candidates.First();

        var roll = random.NextInt64(totalWeight);

        long accumulatedWeight = 0;

        foreach (var candidate in candidates)
        {
            if (candidate.Weight <= 0) continue;
            accumulatedWeight += candidate.Weight;

            if (roll < accumulatedWeight)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Failed to select a round by weight!");
    }

    private void StopPreparation()
    {
        _remainingPreparationTime = 0;
        _countdownSoundPlayed = false;

        notifications?.Clear("ZombiePlague.Round.Preparing");
        _preparationTimer?.Cancel();
        _preparationTimer = null;

        foreach (var respawn in _preparationRespawns.Values)
        {
            respawn.Cancel();
        }

        _preparationRespawns.Clear();

        CancelPreparationSounds();
    }

    private RoundStartResult StartRound(RoundBase round)
    {
        var originalRound = round;

        var preContext = new RoundStartingContext(originalRound.Id);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            StopPreparation();

            NextRound = null;

            DispatchStartRejected(originalRound.Id, RoundStartRejectionReason.Cancelled);

            return RoundStartResult.Cancelled;
        }

        if (preContext.RoundId != originalRound.Id &&
            roundFactory.TryCreate(preContext.RoundId, out var replacementRound) &&
            replacementRound.CanStart()
           )
        {
            round = replacementRound;
        }

        StopPreparation();

        NextRound = null;

        if (!TryStartRoundOrFallback(round, out var startedRound))
        {
            CurrentRound = null;

            // Без подготовки и без раунда погибшие не возрождаются до конца раунда CS2,
            // поэтому неудачный старт возвращает сервер к ожиданию.
            StartPreparationTimer();

            DispatchStartRejected(round.Id, RoundStartRejectionReason.CannotStart);

            return RoundStartResult.CannotStart;
        }

        var postContext = new RoundStartedContext(startedRound);

        hooks.Dispatch(ref postContext);

        return RoundStartResult.Started;
    }

    private bool TryStartRoundOrFallback(RoundBase round, out RoundBase? startedRound)
    {
        if (TryStartRoundInternal(round))
        {
            startedRound = round;

            return true;
        }

        if (round.Id == RoundIds.Infection)
        {
            startedRound = null;

            return false;
        }

        var infection = roundFactory.Create<Infection>();

        if (infection.CanStart() && TryStartRoundInternal(infection))
        {
            startedRound = infection;

            return true;
        }

        startedRound = null;

        return false;
    }

    private void StartPreparationTimer()
    {
        _preparationTimer?.Cancel();

        _remainingPreparationTime = config.Value.PreStartDelay;

        _countdownSoundPlayed = false;

        _preparationTimer = core.Scheduler.DelayAndRepeatBySeconds(
            delaySeconds: DelayPreparationTimer,
            periodSeconds: PeriodSecondsPreparationTask,
            task: OnPrepareTask
        );
    }

    private void HoldCountdown()
    {
        if (_remainingPreparationTime == config.Value.PreStartDelay && !_countdownSoundPlayed)
        {
            return;
        }

        // Игрок ушёл во время отсчёта: следующий участник получит полный отсчёт заново.
        _remainingPreparationTime = config.Value.PreStartDelay;
        _countdownSoundPlayed = false;

        notifications?.Clear("ZombiePlague.Round.Preparing");
        StopSound(ref _countdownSoundEvent);
    }

    private bool IsWaitingForPlayers()
    {
        return CountParticipants(aliveOnly: false) < RequiredPlayers;
    }

    private int CountParticipants(bool aliveOnly)
    {
        return core.PlayerManager
            .GetAllPlayers()
            .Count(player => IsParticipant(player) && (!aliveOnly || player.IsAlive));
    }

    // Участник — полностью подключённый игрок или бот в команде T/CT.
    // Подключающиеся игроки ещё не валидны, зрители находятся вне этих команд.
    private static bool IsParticipant(IPlayer player)
    {
        return player.IsValid && player.Controller.Team is Team.T or Team.CT;
    }

    private static bool IsSpectator(IPlayer player)
    {
        return player.IsValid && player.Controller.Team == Team.Spectator;
    }

    private void RespawnIdleParticipants()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (!IsParticipant(player) || _preparationRespawns.ContainsKey(player.PlayerID))
            {
                continue;
            }

            AdmitPreparationPlayer(player);
        }
    }

    // Игрок в команде T/CT, который так и не получил роль или остался мёртвым
    // (например, вошёл в команду после неудачной инициализации при подключении),
    // становится человеком и возрождается.
    private void AdmitPreparationPlayer(IPlayer player)
    {
        if (!IsPreparing || _nextRoundRequested || !IsParticipant(player))
        {
            return;
        }

        if (!player.IsAlive)
        {
            TryRespawnPlayer(player);
            return;
        }

        if (!playerManager.TryGetRole(player, out _))
        {
            playerManager.TrySetHuman(player);
        }
    }

    // Respawn не создаёт pawn игроку, который ни разу не появлялся на карте. На картах с ботами
    // такой игрок появляется при перезапуске раунда CS2; без ботов перезапуска нет, и подготовка
    // ждала бы вечно. Подготовка ещё не начала режим, поэтому перезапуск ничего не отнимает.
    private bool RestartForUnspawnedParticipants()
    {
        if (_unspawnedRestartCooldown > 0)
        {
            _unspawnedRestartCooldown--;
        }

        var unspawned = core.PlayerManager
            .GetAllPlayers()
            .Where(IsUnspawnedParticipant)
            .ToArray();

        if (unspawned.Length == 0)
        {
            _unspawnedTicks = 0;
            return false;
        }

        if (++_unspawnedTicks < UnspawnedTicksBeforeRestart || _unspawnedRestartCooldown > 0)
        {
            return false;
        }

        core.Logger.LogWarning(
            "[ZombiePlague] Игроки в T/CT не возрождаются во время подготовки ({Players}); раунд CS2 перезапускается",
            string.Join(", ", unspawned.Select(player => $"{player.Name}#{player.PlayerID}")));

        _unspawnedTicks = 0;
        _unspawnedRestartCooldown = UnspawnedRestartCooldownTicks;
        RequestNextRound();

        return true;
    }

    // Игрок T/CT, который после попытки возрождения остался наблюдателем и не ждёт таймера возрождения.
    private bool IsUnspawnedParticipant(IPlayer player)
    {
        return IsParticipant(player) && !player.IsAlive && !_preparationRespawns.ContainsKey(player.PlayerID);
    }

    private void RequestNextRound()
    {
        if (_nextRoundRequested)
        {
            return;
        }

        _nextRoundRequested = true;

        foreach (var respawn in _preparationRespawns.Values)
        {
            respawn.Cancel();
        }

        _preparationRespawns.Clear();

        core.Game.TerminateRound(RoundEndReason.RoundDraw, WaitingRoundRestartDelay);
    }

    public bool TryRespawnPlayer(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive)
        {
            return false;
        }

        if (IsPreparing)
        {
            if (!playerManager.TrySetHuman(player))
            {
                return false;
            }

            return playerManager.TryRespawn(player);
        }

        return CurrentRound?.TryRespawnPlayer(player) ?? false;
    }

    private void ScheduleRespawn(IPlayer? player)
    {
        if (player is not { IsValid: true })
        {
            return;
        }

        var playerId = player.PlayerID;
        var sessionId = player.SessionId;

        if (_preparationRespawns.Remove(playerId, out var previous))
        {
            previous.Cancel();
        }

        CancellationTokenSource? timer = null;
        timer = core.Scheduler.DelayBySeconds(Math.Max(0.05f, config.Value.ZombieSpawnDelay), () =>
        {
            if (!_preparationRespawns.TryGetValue(playerId, out var currentTimer) ||
                !ReferenceEquals(currentTimer, timer)) return;
            _preparationRespawns.Remove(playerId);

            var currentPlayer = core.PlayerManager.GetPlayer(playerId);

            if (currentPlayer is not { IsValid: true } ||
                currentPlayer.SessionId != sessionId)
            {
                return;
            }

            TryRespawnPlayer(currentPlayer);
        });
        _preparationRespawns[playerId] = timer;
    }

    private bool TryStartRoundInternal(RoundBase round)
    {
        CurrentRound = round;

        try
        {
            if (round.TryStart())
            {
                return true;
            }
        }
        catch (Exception exception)
        {
            CurrentRound = null;
            round.End();

            var context = new RoundStartFailedContext(round, exception);
            hooks.Dispatch(ref context);

            throw;
        }

        CurrentRound = null;
        round.End();

        return false;
    }

    private void DispatchStartRejected(string? roundId, RoundStartRejectionReason reason)
    {
        var context = new RoundStartRejectedContext(roundId, reason);
        hooks.Dispatch(ref context);
    }

    private void PlayCountdownSound()
    {
        _countdownSoundEvent = SoundExt.PlayGlobal(config.Value.CountdownSound, config.Value.CountdownSoundVolume);

        _countdownSoundPlayed = true;
    }

    private void CancelPreparationSounds()
    {
        StopSound(ref _countdownSoundEvent);
        StopSound(ref _preparationSoundEvent);
    }

    private void StopSound(ref uint soundEvent)
    {
        if (soundEvent != 0)
        {
            var guid = unchecked((int)soundEvent);

            core.NetMessage.Send<CMsgSosStopSoundEvent>(message =>
            {
                message.SoundeventGuid = guid;
                message.Recipients.AddAllPlayers();
            });
        }

        soundEvent = 0;
    }

    private bool IsWarmupActive()
    {
        var gameRules = core.EntitySystem.GetGameRules();

        return gameRules is not null && gameRules.WarmupPeriod;
    }
}
