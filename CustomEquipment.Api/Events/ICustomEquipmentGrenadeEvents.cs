using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События жизненного цикла пользовательских гранат.
/// </summary>
public interface ICustomEquipmentGrenadeEvents
{
    /// <summary>Вызывается перед выдачей пользовательской гранаты.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Items.Giving`, перед выдачей гранаты</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: типовая отмена отклоняет выдачу</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeGivingContext> Giving { get; }

    /// <summary>Вызывается после успешной выдачи пользовательской гранаты.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После поиска и прикрепления сущности гранаты на следующем world update</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Средний: фактическая выдача завершена</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeGivenContext> Given { get; }

    /// <summary>Вызывается после появления снаряда, но до его регистрации как пользовательской гранаты.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После создания projectile и определения пользовательской гранаты, до установки модели</description></item>
    /// <item><term>Частота</term><description>Часто</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена не удаляет projectile, а отключает его пользовательскую регистрацию</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeThrowingContext> Throwing { get; }

    /// <summary>Вызывается после регистрации брошенной пользовательской гранаты.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После установки модели и регистрации броска</description></item>
    /// <item><term>Частота</term><description>Часто</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Средний: используется контроллером детонации</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeThrownContext> Thrown { get; }

    /// <summary>Вызывается при ожидаемом отказе обработки броска.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отмене `Throwing` или недействительном projectile</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: только аудит отказа</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeThrowRejectedContext> ThrowRejected { get; }

    /// <summary>Вызывается перед пользовательской логикой детонации; её можно отменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Перед удалением projectile и вызовом пользовательской детонации</description></item>
    /// <item><term>Частота</term><description>Часто</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена оставляет штатную дальнейшую судьбу projectile</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeDetonatingContext> Detonating { get; }

    /// <summary>Вызывается после пользовательской логики детонации.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `OnDetonate` пользовательской гранаты</description></item>
    /// <item><term>Частота</term><description>Часто</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Средний: эффекты и урон уже созданы</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeDetonatedContext> Detonated { get; }

    /// <summary>Вызывается при ожидаемом отказе пользовательской детонации.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отмене, неверной подмене, недействительном projectile или thrower</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: пользовательская логика не выполнена</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<GrenadeDetonationRejectedContext> DetonationRejected { get; }
}
