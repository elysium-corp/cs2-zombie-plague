using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События размещения лазерных мин.
/// </summary>
public interface ICustomEquipmentMineEvents
{
    /// <summary>Вызывается перед размещением подготовленной лазерной мины; размещение можно отменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После проверки поверхности и создания сущности, до `Spawn`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена вызывает возврат цены</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<MinePlacingContext> Placing { get; }

    /// <summary>Вызывается после успешного размещения лазерной мины.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `LaserMineEntity.Spawn`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: владелец затем регистрируется внутренним подписчиком</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<MinePlacedContext> Placed { get; }

    /// <summary>Вызывается при ожидаемом отказе размещения лазерной мины.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При неподходящей поверхности, отмене или недействительном игроке</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: модуль запускает возврат цены</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<MinePlacementRejectedContext> PlacementRejected { get; }
}
