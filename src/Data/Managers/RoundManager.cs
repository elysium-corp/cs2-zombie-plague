using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Managers;

public class RoundManager(ISwiftlyCore core, IOptions<ZombiePlagueCoreConfig> coreConfig, IOptions<RoundConfig> roundConfig, IRoundFactory roundFactory)
    : IRoundManager
{
    private readonly ZombieManager _zombieManager = DependencyManager.GetService<ZombieManager>();
    private readonly HumanManager _humanManager = DependencyManager.GetService<HumanManager>();
    private readonly CommonUtils _commonUtils = DependencyManager.GetService<CommonUtils>();
    
    private readonly List<IRound> _rounds = [];
    private IRound? _currentRound;

    private CancellationTokenSource? _token;
    
    private Guid _onRoundStartEvent;
    private Guid _onRoundEndEvent;
    private Guid _onGameRestartEvent;
    private Guid _onPlayerHurtEvent;
    private Guid _onPlayerConnectEvent;
    
    public void RegisterHooks()
    {
        _onRoundStartEvent = core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _onRoundEndEvent = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _onPlayerHurtEvent = core.GameEvent.HookPre<EventPlayerHurt>(OnPlayerHurt);
        _onGameRestartEvent = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);
        _onPlayerConnectEvent = core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null)
        {
            return HookResult.Continue;
        }

        if (IsNoneRound())
        {
            _humanManager.Respawn(player);
        }

        return HookResult.Continue;
    }
    
    private HookResult OnRoundStart(EventRoundStart @event)
    {

        _zombieManager.RemoveAll();
        CancelToken();
        
        _commonUtils.MoveAllPlayersToTeam(Team.CT);
        _commonUtils.AllResetRenderColor();

        SetRound(new None());

        if (RoundIsAvailable())
        {
            Start();
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        return IsNoneRound() ? HookResult.Stop : HookResult.Continue;
    }
    
    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _currentRound?.End();

        return HookResult.Continue;
    }
    
    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        _currentRound?.End();

        return HookResult.Continue;
    }
    
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

    private void CancelToken()
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

    private IRound RandomRound()
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