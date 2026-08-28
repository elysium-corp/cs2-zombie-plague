using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Combat;

namespace ZombiePlague.Api.Events;

/// <summary>События боевой механики Zombie Plague.</summary>
public interface IZombiePlagueCombatEvents
{
    /// <summary>Вызывается перед применением рассчитанного отбрасывания.</summary>
    IHookSubscription<KnockbackApplyingContext> KnockbackApplying { get; }

    /// <summary>Вызывается после применения отбрасывания.</summary>
    IHookSubscription<KnockbackAppliedContext> KnockbackApplied { get; }
}
