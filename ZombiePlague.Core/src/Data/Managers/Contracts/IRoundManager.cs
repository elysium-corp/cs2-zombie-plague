using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Data.Managers.Contracts;

internal interface IRoundManager
{
    public RoundBase? CurrentRound { get; set; }
    
    public RoundBase? NextRound { get; set; }
    
    public void Prepare();

    public void Start();

    public void End();

    public void SelectCurrentRound(RoundBase round);

    public void SelectNextRound(RoundBase round);
    
    public HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        return HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        return HookResult.Continue;
    }

    public virtual void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        
    }
}