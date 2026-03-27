using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;
using ZPApi.Data;
using ZPApi.Events;
using ZPCore.Config.Core;
using ZPCore.Config.Round;
using ZPCore.Data.Rounds;
using ZPCore.Data.Rounds.Contracts;
using ZPCore.Di;
using ZPCore.Utils;
using ZPCore.Utils.Helpers;

namespace ZPCore.Data.Managers;

internal class RoundManager(ISwiftlyCore core, IEventPublisher eventPublisher, IRoundFactory roundFactory, IOptions<ZombiePlagueCoreConfig> coreConfig, IOptions<RoundConfig> roundConfig)
    : IRoundManager
{
    private readonly ZombieManager _zombieManager = DependencyManager.GetService<ZombieManager>();
    private readonly HumanManager _humanManager = DependencyManager.GetService<HumanManager>();
    
    private readonly List<IRound> _rounds = [];
    private IRound _currentRound = new None();

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
    
    public void RegisterRounds()
    {
        _rounds.Clear();

        var roundConfigProperties = roundConfig.Value.GetType()
            .GetProperties();
        
        _rounds.Add(roundFactory.Create(null, this));
        
        foreach (var property in roundConfigProperties)
        {
            var round = (IRoundConfig) property.GetValue(roundConfig.Value);
            
            if (round != null && round.Enable)
            {
                var instance = roundFactory.Create(round, this);
                _rounds.Add(instance);
            }
        }
    }
    
    public bool IsNoneRound()
    {
        return _currentRound is None;
    }
    
    public void SetRound(IRound round)
    {
        _currentRound = round;
    }

    public IRound GetRound()
    {
        return _currentRound;
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
        
        TeamHelper.MoveAllPlayersToTeam(Team.CT);
        RenderColorHelper.AllResetRenderColor();

        SetRound(new None());

        if (IsRoundAvailable())
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

    private void Start()
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
                
                eventPublisher.OnGameRoundStarted(_currentRound);
                
                _currentRound.Start();
                _token?.Cancel();
            }
        });
    }

    private bool IsRoundAvailable()
    {
        var players = core.PlayerManager.GetAllPlayers();

        if (core.EntitySystem.GetGameRules()?.WarmupPeriod == true)
        {
            return false;
        }

        return players.Count() > 1;
    }

    private void CancelToken()
    {
        _token?.Cancel();
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
            totalWeight += round.Chance;
        }

        var randomWeight = Numeric.Random(1, ++totalWeight);
        var currentWeight = 0;
        
        foreach (var round in _rounds)
        {
            currentWeight += round.Chance;
            
            if (randomWeight <= currentWeight)
            {
                return round;
            }
        }

        return roundFactory.Create(roundConfig.Value.Infection, this);
    }
}