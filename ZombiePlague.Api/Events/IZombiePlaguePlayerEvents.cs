using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Player;

namespace ZombiePlague.Api.Events;

/// <summary>
/// События жизненного цикла игроков Zombie Plague.
/// </summary>
public interface IZombiePlaguePlayerEvents
{
    /// <summary>
    /// Вызывается перед заражением игрока. Обработчик может изменить участников или отменить заражение.
    /// </summary>
    IHookSubscription<PlayerInfectingContext> Infecting { get; }

    /// <summary>Вызывается после успешного заражения игрока.</summary>
    IHookSubscription<PlayerInfectedContext> Infected { get; }

    /// <summary>Вызывается перед возвращением зомби в человеческую роль.</summary>
    IHookSubscription<PlayerDisinfectingContext> Disinfecting { get; }

    /// <summary>Вызывается после успешного возвращения зомби в человеческую роль.</summary>
    IHookSubscription<PlayerDisinfectedContext> Disinfected { get; }

    /// <summary>Вызывается перед назначением обычной человеческой роли.</summary>
    IHookSubscription<PlayerHumanizingContext> Humanizing { get; }

    /// <summary>Вызывается после успешного назначения обычной человеческой роли.</summary>
    IHookSubscription<PlayerHumanizedContext> Humanized { get; }

    /// <summary>Вызывается перед назначением роли немезиса.</summary>
    IHookSubscription<PlayerBecomingNemesisContext> BecomingNemesis { get; }

    /// <summary>Вызывается после успешного назначения роли немезиса.</summary>
    IHookSubscription<PlayerBecameNemesisContext> BecameNemesis { get; }

    /// <summary>Вызывается перед назначением роли выжившего.</summary>
    IHookSubscription<PlayerBecomingSurvivorContext> BecomingSurvivor { get; }

    /// <summary>Вызывается после успешного назначения роли выжившего.</summary>
    IHookSubscription<PlayerBecameSurvivorContext> BecameSurvivor { get; }

    /// <summary>Вызывается перед возрождением игрока с назначенной ролью.</summary>
    IHookSubscription<PlayerRespawningContext> Respawning { get; }

    /// <summary>Вызывается после успешного возрождения игрока.</summary>
    IHookSubscription<PlayerRespawnedContext> Respawned { get; }

    /// <summary>Вызывается перед повторным применением назначенной роли.</summary>
    IHookSubscription<PlayerApplyingRoleContext> ApplyingRole { get; }

    /// <summary>Вызывается после успешного повторного применения роли.</summary>
    IHookSubscription<PlayerRoleAppliedContext> RoleApplied { get; }

    /// <summary>Вызывается перед отключением эффектов текущей роли.</summary>
    IHookSubscription<PlayerDeactivatingRoleContext> DeactivatingRole { get; }

    /// <summary>Вызывается после отключения эффектов текущей роли.</summary>
    IHookSubscription<PlayerRoleDeactivatedContext> RoleDeactivated { get; }
}
