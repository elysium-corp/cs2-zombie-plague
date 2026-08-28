using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Combat;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlagueCombatEvents(IHookSubscriber hooks) : IZombiePlagueCombatEvents
{
    public IHookSubscription<KnockbackApplyingContext> KnockbackApplying { get; } =
        new HookEvent<KnockbackApplyingContext>(hooks);

    public IHookSubscription<KnockbackAppliedContext> KnockbackApplied { get; } =
        new HookEvent<KnockbackAppliedContext>(hooks);
}
