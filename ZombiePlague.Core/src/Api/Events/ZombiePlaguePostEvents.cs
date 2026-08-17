using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePostEvents(IHookSubscriber hooks ) : IZombiePlaguePostEvents
{
    public event HookHandler<PlayerInfectPostContext> PlayerInfectEvent
    {
        add => hooks.Hook(value);
        remove => hooks.Unhook(value);
    }
    
    public event HookHandler<RoundStartPostContext> RoundStartEvent
    {
        add => hooks.Hook(value);
        remove => hooks.Unhook(value);
    }
}