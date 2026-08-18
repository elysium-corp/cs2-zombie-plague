using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Data.Managers.Contracts;

internal interface IRoundManager
{
    RoundBase? CurrentRound { get; }

    RoundBase? NextRound { get; }

    void Prepare();

    void Start();

    void End();

    bool TryStartRound(RoundBase round);
    
    void SelectNextRound(RoundBase round);

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