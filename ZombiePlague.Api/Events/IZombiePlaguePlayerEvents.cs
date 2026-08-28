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
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TryInfect`, после первичной проверки цели и до создания роли зомби</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена или подмена `Player`/`Infector` меняет заражение</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerInfectingContext> Infecting { get; }

    /// <summary>Вызывается после успешного заражения игрока.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После установки роли зомби и показа эффекта заражения</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: подходит для наград и статистики; не запускать повторное заражение синхронно</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerInfectedContext> Infected { get; }

    /// <summary>Вызывается перед возвращением зомби в человеческую роль.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TryDisinfect`, до создания человеческой роли</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена оставляет роль зомби</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerDisinfectingContext> Disinfecting { get; }

    /// <summary>Вызывается после успешного возвращения зомби в человеческую роль.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После успешной замены роли на человеческую</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: роль уже применена</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerDisinfectedContext> Disinfected { get; }

    /// <summary>Вызывается перед назначением обычной человеческой роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TrySetHuman`, до создания обычной человеческой роли</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Высокий: во время подготовки вызывается для всех игроков; обработчик должен быть O(1)</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerHumanizingContext> Humanizing { get; }

    /// <summary>Вызывается после успешного назначения обычной человеческой роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После успешного назначения обычной человеческой роли</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Средний: возможна серия вызовов на старте подготовки</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerHumanizedContext> Humanized { get; }

    /// <summary>Вызывается перед назначением роли немезиса.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TrySetNemesis`, до создания специальной роли</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена может сорвать сценарий специального раунда</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerBecomingNemesisContext> BecomingNemesis { get; }

    /// <summary>Вызывается после успешного назначения роли немезиса.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После установки роли немезиса</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: роль уже активна</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerBecameNemesisContext> BecameNemesis { get; }

    /// <summary>Вызывается перед назначением роли выжившего.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TrySetSurvivor`, до создания специальной роли</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена может сорвать сценарий специального раунда</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerBecomingSurvivorContext> BecomingSurvivor { get; }

    /// <summary>Вызывается после успешного назначения роли выжившего.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После установки роли выжившего</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: роль уже активна</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerBecameSurvivorContext> BecameSurvivor { get; }

    /// <summary>Вызывается перед возрождением игрока с назначенной ролью.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TryRespawn`, после проверки смерти и наличия роли, до `Respawn()`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: подменённый игрок повторно валидируется</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerRespawningContext> Respawning { get; }

    /// <summary>Вызывается после успешного возрождения игрока.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Сразу после вызова `Respawn()`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: движок может завершать часть spawn-логики позднее</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerRespawnedContext> Respawned { get; }

    /// <summary>Вызывается перед повторным применением назначенной роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TryApplyRole`, до `Unbind`, смены команды и `Bind`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: влияет на команду, класс и способности</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerApplyingRoleContext> ApplyingRole { get; }

    /// <summary>Вызывается после успешного повторного применения роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После смены команды и привязки текущей роли</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: не вызывать рекурсивно `TryApplyRole`</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerRoleAppliedContext> RoleApplied { get; }

    /// <summary>Вызывается перед отключением эффектов текущей роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerManager.TryDeactivateRole`, до `Unbind`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена сохраняет эффекты роли</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerDeactivatingRoleContext> DeactivatingRole { get; }

    /// <summary>Вызывается после отключения эффектов текущей роли.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Unbind` текущей роли</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: запись роли остаётся в менеджере</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<PlayerRoleDeactivatedContext> RoleDeactivated { get; }
}
