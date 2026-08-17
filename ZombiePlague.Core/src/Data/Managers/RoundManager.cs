using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
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
    IRoundFactory roundFactory
) : IRoundManager
{
    public RoundBase? CurrentRound { get; set; }
    public RoundBase? NextRound { get; set; }

    private CancellationTokenSource? _preparationTimer;
    private int _remainingPreparationTime;
    private bool _countdownSoundPlayed;

    private const float DelayPreparationTimer = 1.5f;
    private const int PeriodSecondsPreparationTask = 1;

    private const string RoundStartSoundName = "ZombiePlagueSounds.round_start";
    
    public void Prepare()
    {
        CurrentRound = null;
        
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

        if (_preparationTimer != null)
        {
            _preparationTimer?.Cancel();
            _preparationTimer = null;
        }
        
        _preparationTimer = core.Scheduler.DelayAndRepeatBySeconds(
            delaySeconds: DelayPreparationTimer, 
            periodSeconds: PeriodSecondsPreparationTask, 
            task: OnPrepareTask
        );
    }
    
    public void Start()
    {
        _remainingPreparationTime = 0;
        _countdownSoundPlayed = false;
        
        _preparationTimer?.Cancel();
        _preparationTimer = null;
    
        var round = CreateRandomRoundOrDefault();

        round.Start();

        try
        {
            round.Start();
        }
        catch
        {
            CurrentRound = null;
            throw;
        }
    }

    public void End()
    {
        if (_preparationTimer != null)
        {
            _preparationTimer.Cancel();
            _preparationTimer = null;
        }
        
        CurrentRound?.End();
        CurrentRound = null;
    }

    public void SelectCurrentRound(RoundBase round)
    {
        CurrentRound = round;
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
        if (_preparationTimer == null)
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
    
    private RoundBase CreateRandomRoundOrDefault()
    {
        if (CurrentRound != null)
        {
            return CurrentRound;
        }

        if (NextRound != null)
        {
            var round = NextRound;
            NextRound = null;
            return round;
        }
        
        var candidates = roundRegistrator.GetAllEnabled().ToList();

        while (candidates.Count > 0)
        {
            var selectedConfig = SelectByWeight(candidates, Random.Shared);

            // Чтобы неподходящий раунд больше не выпадал.
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
        var totalWeight = candidates.Sum(
            static round => (long)round.Weight
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