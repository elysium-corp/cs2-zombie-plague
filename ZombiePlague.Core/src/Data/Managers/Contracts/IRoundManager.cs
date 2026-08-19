using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Data.Managers.Contracts;

internal interface IRoundManager
{
    RoundBase? CurrentRound { get; }

    RoundBase? NextRound { get; }
    
    bool IsPreparing { get; }

    void Prepare();

    void Start();

    void End();

    RoundStartResult TryStartRound(RoundBase round);
    
    RoundStartResult TryStartRandomRound();
    
    void SelectNextRound(RoundBase round);
    
    void ClearNextRound();

    HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        return HookResult.Continue;
    }

    HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        return HookResult.Continue;
    }

    HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        return HookResult.Continue;
    }

    HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        return HookResult.Continue;
    }

    void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
    }
}