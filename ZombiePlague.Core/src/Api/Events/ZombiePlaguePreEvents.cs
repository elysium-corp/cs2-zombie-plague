using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePreEvents(IHookSubscriber hooks) : IZombiePlaguePreEvents
{
    // - Player Events
    private readonly HookEvent<PlayerInfectPreContext> _playerInfect = new(hooks);
    
    public IHookSubscription<PlayerInfectPreContext> PlayerInfect => _playerInfect;

    public event HookHandler<PlayerInfectPreContext> PlayerInfectEvent
    {
        add => _playerInfect.Event += value;
        remove => _playerInfect.Event -= value;
    }
    
    // - Round Events
    private readonly HookEvent<RoundStartPreContext> _roundStart = new(hooks);
    
    public IHookSubscription<RoundStartPreContext> RoundStart => _roundStart;

    public event HookHandler<RoundStartPreContext> RoundStartEvent
    {
        add => _roundStart.Event += value;
        remove => _roundStart.Event -= value;
    }
}