using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
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
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class RoundManager(
    ISwiftlyCore core,
    IOptions<ZombiePlagueCoreConfig> coreConfig,
    IPlayerManager playerManager,
    IRoundRegistrator roundRegistrator,
    IRoundFactory roundFactory,
    IHookPublisher hooks
) : IRoundManager
{
    public RoundBase? CurrentRound { get; private set; }

    public RoundBase? NextRound { get; private set; }

    public bool IsPreparing => _preparationTimer is not null;

    private CancellationTokenSource? _preparationTimer;

    private int _remainingPreparationTime;

    private bool _countdownSoundPlayed;
    private uint _countdownSoundEvent;
    private uint _preparationSoundEvent;

    private const float DelayPreparationTimer = 1.5f;

    private const int PeriodSecondsPreparationTask = 1;

    private const string RoundStartSoundName = "ZombiePlagueSounds.round_start";

    public void Prepare()
    {
        End();

        if (IsWarmupActive())
        {
            return;
        }

        _preparationSoundEvent = SoundExt.PlayGlobal(RoundStartSoundName, 1.5f);

        var allPlayers = core.PlayerManager.GetAllPlayers();

        foreach (var player in allPlayers)
        {
            playerManager.TrySetHuman(player);
        }

        _remainingPreparationTime = coreConfig.Value.PreStartDelay;

        _countdownSoundPlayed = false;

        _preparationTimer = core.Scheduler.DelayAndRepeatBySeconds(
            delaySeconds: DelayPreparationTimer,
            periodSeconds: PeriodSecondsPreparationTask,
            task: OnPrepareTask
        );
    }

    public void Start()
    {
        if (_preparationTimer is null)
        {
            return;
        }

        var round = TakeNextRound() ?? CreateRandomRoundOrDefault();

        StartRound(round);
    }

    public RoundStartResult TryStartRound(RoundBase round)
    {
        if (_preparationTimer is null)
        {
            return RoundStartResult.NotPreparing;
        }

        if (!round.CanStart())
        {
            return RoundStartResult.CannotStart;
        }

        return StartRound(round);
    }

    public RoundStartResult TryStartRandomRound()
    {
        if (!IsPreparing)
        {
            return RoundStartResult.NotPreparing;
        }

        var round = CreateRandomRoundOrDefault();

        return StartRound(round);
    }

    public void End()
    {
        StopPreparation();

        var round = CurrentRound;

        if (round is null)
        {
            return;
        }

        try
        {
            round.End();
        }
        finally
        {
            CurrentRound = null;
        }
    }

    public void SelectNextRound(RoundBase round)
    {
        NextRound = round;
    }

    public void ClearNextRound()
    {
        NextRound = null;
    }

    public HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        return CurrentRound?.HandlePlayerConnectedFull(@event) ?? HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        if (IsPreparing)
        {
            ScheduleRespawn(@event.UserIdPlayer);

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
        return CurrentRound?.HandlePlayerTeam(@event) ?? HookResult.Continue;
    }

    public void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim is { IsValid: true } && playerManager.IsZombie(victim) && (context.Params.Info.DamageType & DamageTypes_t.DMG_FALL) != 0)
        {
            context.Params.Info.Damage = 0;
            context.SetHookResult(HookResult.CancelOriginal);

            return;
        }

        CurrentRound?.HandleTakeDamage(ref context);
    }

    private void OnPrepareTask()
    {
        if (_preparationTimer is null)
        {
            return;
        }

        _remainingPreparationTime--;

        if (!_countdownSoundPlayed && _remainingPreparationTime == 10)
        {
            PlayCountdownSound();
        }

        if (_remainingPreparationTime < 1)
        {
            Start();

            return;
        }

        core.PlayerManager.SendCenterAsync($"До заражения {_remainingPreparationTime} секунд");
    }

    private RoundBase? TakeNextRound()
    {
        if (NextRound is null)
        {
            return null;
        }

        return NextRound.CanStart() ? NextRound : null;
    }

    private RoundBase CreateRandomRoundOrDefault()
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

        return roundFactory.Create<Infection>();
    }

    private static IRoundConfig SelectByWeight(IReadOnlyCollection<IRoundConfig> candidates, Random random)
    {
        var totalWeight = candidates.Sum(static round => (long)round.Weight
        );

        var roll = random.NextInt64(totalWeight);

        long accumulatedWeight = 0;

        foreach (var candidate in candidates)
        {
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

        _preparationTimer?.Cancel();
        _preparationTimer = null;

        CancelPreparationSounds();
    }

    private RoundStartResult StartRound(RoundBase round)
    {
        var originalRound = round;

        var preContext = new RoundStartPreContext(originalRound.Id);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            StopPreparation();

            NextRound = null;

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

            return RoundStartResult.CannotStart;
        }

        var postContext = new RoundStartPostContext(startedRound);

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

        if (TryStartRoundInternal(infection))
        {
            startedRound = infection;

            return true;
        }

        startedRound = null;

        return false;
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

        core.Scheduler.DelayBySeconds(coreConfig.Value.ZombieSpawnDelay, () =>
        {
            TryRespawnPlayer(player);
        });
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
        catch
        {
            CurrentRound = null;

            throw;
        }

        CurrentRound = null;

        return false;
    }

    private void PlayCountdownSound()
    {
        _countdownSoundEvent = SoundExt.PlayGlobal("ZombiePlagueSounds.countdown", 2f);

        _countdownSoundPlayed = true;
    }

    private void CancelPreparationSounds()
    {
        if (_countdownSoundEvent != 0)
        {
            core.NetMessage.Send<CMsgSosStopSoundEvent>(message =>
            {
                message.SoundeventGuid = unchecked((int)_countdownSoundEvent);
                message.Recipients.AddAllPlayers();
            });
        }

        if (_preparationSoundEvent != 0)
        {
            core.NetMessage.Send<CMsgSosStopSoundEvent>(message =>
            {
                message.SoundeventGuid = unchecked((int)_preparationSoundEvent);
                message.Recipients.AddAllPlayers();
            });
        }

        _countdownSoundEvent = 0;
        _preparationSoundEvent = 0;
    }

    private bool IsWarmupActive()
    {
        var gameRules = core.EntitySystem.GetGameRules();

        return gameRules is not null && gameRules.WarmupPeriod;
    }
}