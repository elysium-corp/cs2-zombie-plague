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

    public void RegisterRounds()
    {
        _rounds.Clear();
        _rounds.Add(roundFactory.Create(null, this));
        foreach (var round in roundConfig.Value.Rounds)
        {
            var instance = roundFactory.Create(round, this);
            if (round.Enable && !_rounds.Contains(instance))
            {
                _rounds.Add(instance);
            }
        }
    }

    public void Start()
    {
        var roundStartTime = coreConfig.Value.PreStartDelay;
        var localTime = 0;
        bool soundIsActive = false;

        StartRoundMusic();
            
        _token = core.Scheduler.RepeatBySeconds(1, () =>
        {
            localTime += 1;
            core.PlayerManager.SendCenterAsync("До заражения " + (roundStartTime - localTime) + " секунд");

            if (roundStartTime - localTime <= 11 && !soundIsActive)
            {
                soundIsActive = StartCountdownSound();
            }

            if (localTime >= roundStartTime)
            {
                if (_currentRound is None)
                {
                    SetRound(RandomRound());
                }

                _currentRound!.Start();
                _token?.Cancel();
            }
        });
    }

    public bool RoundIsAvailable()
    {
        var players = core.PlayerManager.GetAllPlayers();

        if (core.EntitySystem.GetGameRules()?.WarmupPeriod == true)
        {
            return false;
        }

        return players.Count() > 1;
    }

    public bool IsNoneRound()
    {
        return _currentRound is None;
    }

    public void CancelToken()
    {
        if (_token != null)
        {
            _token.Cancel();
        }
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

    private IRound RandomRound()
    {
        var totalWeight = 0;
        foreach (var config in roundConfig.Value.Rounds)
        {
            totalWeight += config.Chance;
        }

        var randomizer = new Random();
        var randomWeight = randomizer.Next(0, totalWeight + 1);

        var currentWeight = 0;
        foreach (var config in roundConfig.Value.Rounds)
        {
            currentWeight += config.Chance;
            if (randomWeight <= currentWeight)
            {
                return roundFactory.Create(config, this);
            }
        }

        return roundFactory.Create(roundConfig.Value.Rounds.Find(r => r is InfectionConfig), this);
    }
}