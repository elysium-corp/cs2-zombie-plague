using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
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

    private CancellationTokenSource? _preparationTimer;

    private int _remainingPreparationTime;

    private bool _countdownSoundPlayed;

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

        SoundExt.PlayGlobal(RoundStartSoundName, 1.5f);

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

        if (round is null)
        {
            StopPreparation();

            return;
        }

        StartRound(round);
    }
    
    public bool TryStartRound(RoundBase round)
    {
        if (_preparationTimer is null)
        {
            return false;
        }

        if (!round.CanStart())
        {
            return false;
        }

        StartRound(round);

        return true;
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

    public HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        return CurrentRound?.HandlePlayerConnectedFull(@event) ?? HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
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

    private RoundBase? CreateRandomRoundOrDefault()
    {
        var candidates = roundRegistrator
            .GetAllEnabled()
            .ToList();

        while (candidates.Count > 0)
        {
            var selectedConfig = SelectByWeight(candidates, Random.Shared);

            candidates.Remove(selectedConfig);

            var selectedRound =
                roundFactory.Create(selectedConfig);

            if (selectedRound.CanStart())
            {
                return selectedRound;
            }
        }

        var infectionRound = roundFactory.Create<Infection>();

        return infectionRound.CanStart()
            ? infectionRound
            : null;
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
    }
    
    private bool StartRound(RoundBase round)
    {
        StopPreparation();

        NextRound = null;

        var preContext = new RoundStartPreContext(round.Id);

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            return false;
        }

        if (preContext.RoundId != round.Id)
        {
            if (!roundFactory.TryCreate(preContext.RoundId, out var replacementRound))
            {
                return false;
            }

            round = replacementRound;
        }

        if (!round.CanStart())
        {
            return false;
        }

        CurrentRound = round;

        try
        {
            round.Start();
        }
        catch
        {
            CurrentRound = null;

            throw;
        }

        var postContext = new RoundStartPostContext(round);

        hooks.Dispatch(ref postContext);

        return true;
    }

    private void PlayCountdownSound()
    {
        SoundExt.PlayGlobal("ZombiePlagueSounds.countdown", 2f);

        _countdownSoundPlayed = true;
    }

    private bool IsWarmupActive()
    {
        var gameRules = core.EntitySystem.GetGameRules();

        return gameRules is not null && gameRules.WarmupPeriod;
    }
}