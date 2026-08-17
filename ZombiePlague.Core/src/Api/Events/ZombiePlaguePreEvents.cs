using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePreEvents(IHookSubscriber hooks) : IZombiePlaguePreEvents
{
    public event HookHandler<PlayerInfectPreContext> PlayerInfectEvent
    {
        add => hooks.Hook(value);
        remove => hooks.Unhook(value);
    }
}