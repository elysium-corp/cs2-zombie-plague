using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePreEvents(IHookSubscriber hooks) : IZombiePlaguePreEvents
{
    private readonly HookEvent<PlayerInfectPreContext> _playerInfect = new(hooks);

    public IHookSubscription<PlayerInfectPreContext> PlayerInfect => _playerInfect;

    public event HookHandler<PlayerInfectPreContext> PlayerInfectEvent
    {
        add => _playerInfect.Event += value;
        remove => _playerInfect.Event -= value;
    }
}