using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Api.Events;

public interface IZombiePlaguePreEvents
{
    event HookHandler<PlayerInfectPreContext> PlayerInfectEvent;

    IHookSubscription<PlayerInfectPreContext> PlayerInfect { get; }
}