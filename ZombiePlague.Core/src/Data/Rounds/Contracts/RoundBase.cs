using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class RoundBase(ISwiftlyCore core, IPlayerManager playerManager) : IRound
{
    public abstract string Id { get; }
    
    public abstract string Name { get; }

    protected IPlayerManager PlayerManager { get; } = playerManager;

    protected ISwiftlyCore Core { get; } = core;

    private bool _isRoundEnded;

    protected abstract bool OnStart();

    protected abstract void OnEnd();

    public virtual void Start()
    {
        TryStart();
    }
    
    public bool TryStart()
    {
        _isRoundEnded = false;

        if (OnStart())
        {
            return true;
        }

        _isRoundEnded = true;

        return false;
    }

    public virtual void End()
    {
        _isRoundEnded = true;
        
        OnEnd();
    }

    public virtual bool CanStart()
    {
        return true;
    }

    protected virtual Team? DetermineWinner()
    {
        var hasAliveZombies = PlayerManager
            .GetAllAliveZombies()
            .Any();

        var hasAliveHumans = PlayerManager
            .GetAllAliveHumans()
            .Any();

        return (hasAliveHumans, hasAliveZombies) switch
        {
            (true, false) => Team.CT,
            (false, true) => Team.T,

            // - обе стороны живы либо на сервере никого нет
            _ => null
        };
    }
    
    public virtual bool TryRespawnPlayer(IPlayer player)
    {
        return false;
    }

    private void TryRequestRoundEnd()
    {
        if (_isRoundEnded)
        {
            return;
        }

        var winner = DetermineWinner();

        if (winner is null)
        {
            return;
        }

        var reason = winner.Value switch
        {
            Team.T => RoundEndReason.TerroristsWin,
            Team.CT => RoundEndReason.CTsWin,

            _ => throw new InvalidOperationException(
                $"Unsupported winning team: {winner}."
            )
        };

        _isRoundEnded = true;

        Core.Game.TerminateRound(reason, 5.0f);
    }

    public HookResult HandlePlayerConnectedFull(EventPlayerConnectFull @event)
    {
        if (_isRoundEnded)
        {
            return HookResult.Continue;
        }

        return OnPlayerConnectedFull(@event);
    }

    public HookResult HandlePlayerDeath(EventPlayerDeath @event)
    {
        if (_isRoundEnded)
        {
            return HookResult.Continue;
        }

        var result = OnPlayerDeath(@event);

        TryRequestRoundEnd();

        return result;
    }

    public HookResult HandlePlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (_isRoundEnded)
        {
            return HookResult.Continue;
        }

        var result = OnPlayerDisconnect(@event);

        TryRequestRoundEnd();

        return result;
    }

    public HookResult HandlePlayerTeam(EventPlayerTeam @event)
    {
        if (_isRoundEnded)
        {
            return HookResult.Continue;
        }

        var result = OnPlayerTeam(@event);

        TryRequestRoundEnd();

        return result;
    }

    public void HandleTakeDamage(ref TakeDamageEntityPreContext context)
    {
        if (_isRoundEnded)
        {
            return;
        }

        OnTakeDamage(ref context);
    }

    protected virtual HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event)
    {
        return HookResult.Continue;
    }

    protected virtual HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        return HookResult.Continue;
    }

    protected virtual HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        return HookResult.Continue;
    }

    protected virtual HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        return HookResult.Continue;
    }

    protected virtual void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
    }
}