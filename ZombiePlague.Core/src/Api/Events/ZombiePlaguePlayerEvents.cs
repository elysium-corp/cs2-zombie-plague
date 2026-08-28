using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Player;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlaguePlayerEvents(IHookSubscriber hooks) : IZombiePlaguePlayerEvents
{
    public IHookSubscription<PlayerInfectingContext> Infecting { get; } =
        new HookEvent<PlayerInfectingContext>(hooks);

    public IHookSubscription<PlayerInfectedContext> Infected { get; } =
        new HookEvent<PlayerInfectedContext>(hooks);

    public IHookSubscription<PlayerDisinfectingContext> Disinfecting { get; } =
        new HookEvent<PlayerDisinfectingContext>(hooks);

    public IHookSubscription<PlayerDisinfectedContext> Disinfected { get; } =
        new HookEvent<PlayerDisinfectedContext>(hooks);

    public IHookSubscription<PlayerHumanizingContext> Humanizing { get; } =
        new HookEvent<PlayerHumanizingContext>(hooks);

    public IHookSubscription<PlayerHumanizedContext> Humanized { get; } =
        new HookEvent<PlayerHumanizedContext>(hooks);

    public IHookSubscription<PlayerBecomingNemesisContext> BecomingNemesis { get; } =
        new HookEvent<PlayerBecomingNemesisContext>(hooks);

    public IHookSubscription<PlayerBecameNemesisContext> BecameNemesis { get; } =
        new HookEvent<PlayerBecameNemesisContext>(hooks);

    public IHookSubscription<PlayerBecomingSurvivorContext> BecomingSurvivor { get; } =
        new HookEvent<PlayerBecomingSurvivorContext>(hooks);

    public IHookSubscription<PlayerBecameSurvivorContext> BecameSurvivor { get; } =
        new HookEvent<PlayerBecameSurvivorContext>(hooks);

    public IHookSubscription<PlayerRespawningContext> Respawning { get; } =
        new HookEvent<PlayerRespawningContext>(hooks);

    public IHookSubscription<PlayerRespawnedContext> Respawned { get; } =
        new HookEvent<PlayerRespawnedContext>(hooks);

    public IHookSubscription<PlayerApplyingRoleContext> ApplyingRole { get; } =
        new HookEvent<PlayerApplyingRoleContext>(hooks);

    public IHookSubscription<PlayerRoleAppliedContext> RoleApplied { get; } =
        new HookEvent<PlayerRoleAppliedContext>(hooks);

    public IHookSubscription<PlayerDeactivatingRoleContext> DeactivatingRole { get; } =
        new HookEvent<PlayerDeactivatingRoleContext>(hooks);

    public IHookSubscription<PlayerRoleDeactivatedContext> RoleDeactivated { get; } =
        new HookEvent<PlayerRoleDeactivatedContext>(hooks);
}
