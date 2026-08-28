using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Player;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlagueClassEvents(IHookSubscriber hooks) : IZombiePlagueClassEvents
{
    public IHookSubscription<ClassSelectingContext> Selecting { get; } =
        new HookEvent<ClassSelectingContext>(hooks);

    public IHookSubscription<ClassSelectedContext> Selected { get; } =
        new HookEvent<ClassSelectedContext>(hooks);

    public IHookSubscription<ClassSelectionRejectedContext> SelectionRejected { get; } =
        new HookEvent<ClassSelectionRejectedContext>(hooks);
}
