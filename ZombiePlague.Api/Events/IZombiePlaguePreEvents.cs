using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Api.Events;

public interface IZombiePlaguePreEvents
{
    event HookHandler<PlayerInfectPreContext> PlayerInfectEvent;

    IHookSubscription<PlayerInfectPreContext> PlayerInfect { get; }
    
    event HookHandler<RoundStartPreContext> RoundStartEvent;

    IHookSubscription<RoundStartPreContext> RoundStart { get; }
}