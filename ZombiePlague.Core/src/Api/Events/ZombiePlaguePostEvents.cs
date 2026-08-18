using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePostEvents(IHookSubscriber hooks) : IZombiePlaguePostEvents
{
    private readonly HookEvent<PlayerInfectPostContext> _playerInfect = new(hooks);
    private readonly HookEvent<RoundStartPostContext> _roundStart = new(hooks);
    
    public IHookSubscription<PlayerInfectPostContext> PlayerInfect => _playerInfect;

    public event HookHandler<PlayerInfectPostContext> PlayerInfectEvent
    {
        add => _playerInfect.Event += value;
        remove => _playerInfect.Event -= value;
    }
    
    public IHookSubscription<RoundStartPostContext> RoundStart => _roundStart;

    public event HookHandler<RoundStartPostContext> RoundStartEvent
    {
        add => _roundStart.Event += value;
        remove => _roundStart.Event -= value;
    }
}