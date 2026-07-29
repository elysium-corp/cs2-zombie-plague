using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;
using ZombiePlague.Core.Utils.Helpers;
using ZPCore.Config.Core;
using ZPCore.Config.Round;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class RoundManager(
    ISwiftlyCore core,
    IEventPublisher eventPublisher,
    IRoundFactory roundFactory,
    IOptions<ZombiePlagueCoreConfig> coreConfig,
    IOptions<RoundConfig> roundConfig,
    IZombieManager zombieManager,
    IHumanManager humanManager
    )
{
    private readonly List<IRound> _rounds = [];
    private IRound _currentRound = new None();

    private CancellationTokenSource? _token;
    private int _preRoundTime;
    private bool _countdownSoundActive;
    private Guid _onRoundStartEvent;
    private Guid _onRoundEndEvent;
    private Guid _onGameRestartEvent;
    private Guid _onPlayerConnectFullEvent;
    private bool _hooksRegistered;

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _onRoundStartEvent = core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _onRoundEndEvent = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _onGameRestartEvent = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);
        _onPlayerConnectFullEvent = core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        CancelToken();
        EndCurrentRound();
        core.GameEvent.Unhook(_onRoundStartEvent);
        core.GameEvent.Unhook(_onRoundEndEvent);
        core.GameEvent.Unhook(_onGameRestartEvent);
        core.GameEvent.Unhook(_onPlayerConnectFullEvent);
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
        _hooksRegistered = false;
    }

    public List<IRound> GetRegisteredRounds()
    {
        return _rounds;
    }

    public void RegisterRounds()
    {
        _rounds.Clear();

        var roundConfigProperties = roundConfig.Value.GetType()
            .GetProperties();

        _rounds.Add(roundFactory.Create(null));

        foreach (var property in roundConfigProperties)
        {
            if (property.GetValue(roundConfig.Value) is IRoundConfig { Enable: true } round)
            {
                _rounds.Add(roundFactory.Create(round));
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
            humanManager.Respawn(player);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        EndCurrentRound();
        zombieManager.RemoveAll();
        CancelToken();

        TeamHelper.MoveAllPlayersToTeam(core, Team.CT);
        RenderColorHelper.AllResetRenderColor(core);

        if (IsRoundAvailable())
        {
            StartPreRound();
        }

        return HookResult.Continue;
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
    {
        if (IsNoneRound())
        {
            @event.Params.Info.Damage = 0;
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        EndCurrentRound();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        EndCurrentRound();

        return HookResult.Continue;
    }

    private void StartPreRound()
    {
        _preRoundTime = 0;
        _countdownSoundActive = false;

        SoundExt.PlayGlobal("ZombiePlagueSounds.round_start", 2f);

        var roundStartTime = coreConfig.Value.PreStartDelay;
        _token = core.Scheduler.RepeatBySeconds(1, () => OnPreRoundTick(roundStartTime));
    }

    private void OnPreRoundTick(int roundStartTime)
    {
        _preRoundTime += 1;

        core.PlayerManager.SendCenterAsync("До заражения " + (roundStartTime - _preRoundTime) + " секунд");

        if (roundStartTime - _preRoundTime <= 11 && !_countdownSoundActive)
        {
            SoundExt.PlayGlobal("ZombiePlagueSounds.countdown", 3f);
            _countdownSoundActive = true;
        }

        if (_preRoundTime < roundStartTime)
        {
            return;
        }

        if (IsNoneRound())
        {
            SetRound(ResolveRandomRound());
        }

        _currentRound.Start();
        CancelToken();

        eventPublisher.OnGameRoundStarted(_currentRound);
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
        _token = null;
    }

    private IRound ResolveRandomRound()
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

        return roundFactory.Create(roundConfig.Value.Infection);
    }

    private void EndCurrentRound()
    {
        if (_currentRound is None)
        {
            return;
        }

        _currentRound.End();
        _currentRound = new None();
    }
}
