using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Api.Events;

public interface IZombiePlaguePostEvents
{
    event HookHandler<PlayerInfectPostContext> PlayerInfectEvent;

    IHookSubscription<PlayerInfectPostContext> PlayerInfect { get; }

    event HookHandler<RoundStartPostContext> RoundStartEvent;

    IHookSubscription<RoundStartPostContext> RoundStart { get; }
}