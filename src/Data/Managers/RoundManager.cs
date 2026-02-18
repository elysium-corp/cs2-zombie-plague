using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Rounds;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Managers;

public class RoundManager(ISwiftlyCore core, IOptions<ZombiePlagueCoreConfig> coreConfig, IOptions<RoundConfig> roundConfig, IRoundFactory roundFactory)
    : IRoundManager
{
    private readonly List<IRound> _rounds = [];
    private IRound? _currentRound;

    private CancellationTokenSource? _token;
    private const int TimerTickInterval = 1;

    public void RegisterRounds()
    {
        _rounds.Clear();
        
        _rounds.Add(roundFactory.Create(null, this));
        
        List<IRoundConfig> rounds =
        [
            roundConfig.Value.Infection,
            roundConfig.Value.Plague,
            roundConfig.Value.Nemesis,
            roundConfig.Value.Survivor,
            roundConfig.Value.Armageddon
        ]; 
        
        foreach (var round in rounds)
        {
            var instance = roundFactory.Create(round, this);
            if (round.Enable && !_rounds.Contains(instance))
            {
                _rounds.Add(instance);
            }
        }
    }

    public void StartGameCountdown()
    {
        StartRoundMusic();
            
        StartCountdownTimer();
    }

    public bool RoundIsAvailable()
    {
        var players = core.PlayerManager.GetAllPlayers();
        var gameRules = core.EntitySystem.GetGameRules();
        
        if (gameRules == null)
        {
            return false;
        }

        return players.Count() > 1 && !gameRules.WarmupPeriod;
    }

    public bool IsNoneRound()
    {
        return _currentRound is None;
    }

    public void CancelToken()
    {
        _token?.Cancel();
    }

    public void SetRound(IRound round)
    {
        _currentRound = round;
    }

    public IRound? GetRound()
    {
        return _currentRound;
    }

    private void StartRoundMusic()
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 2,
            Name = "ZombiePlagueSounds.round_start",
            SourceEntityIndex = -1
        };
        
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
    }
    
    private bool StartCountdownSound()
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 3,
            Name = "ZombiePlagueSounds.countdown",
            SourceEntityIndex = -1
        };
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();

        return true;
    }

    private void StartCountdownTimer()
    {   
        var roundStartTime = coreConfig.Value.PreStartDelay;
        var currentTime = 0;
        var soundIsActive = false;
        
        _token = core.Scheduler.RepeatBySeconds(TimerTickInterval, () =>
        {
            currentTime += TimerTickInterval;

            var timeUntilStart = roundStartTime - currentTime;
            
            core.PlayerManager.SendCenterAsync("До заражения " + (timeUntilStart) + " секунд");

            if (timeUntilStart <= 11 && !soundIsActive)
            {
                soundIsActive = StartCountdownSound();
            }

            if (timeUntilStart <= 0)
            {
                if (_currentRound is None)
                {
                    SetRound(GenerateRandomRound());
                }

                _currentRound?.Start();
                _token?.Cancel();
            }
        });
    }
    
    private IRound GenerateRandomRound()
    {
        var totalWeight = 0;
        foreach (var round in _rounds)
        {
            totalWeight += round.GetChance();
        }
        
        var randomizer = new Random();
        var randomWeight = randomizer.Next(1, totalWeight + 1);

        var currentWeight = 0;
        foreach (var round in _rounds)
        {
            currentWeight += round.GetChance();
            if (randomWeight <= currentWeight)
            {
                return round;
            }
        }

        return roundFactory.Create(roundConfig.Value.Infection, this);
    }
}