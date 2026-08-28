using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События выдачи пользовательского оружия.
/// </summary>
public interface ICustomEquipmentWeaponEvents
{
    /// <summary>Вызывается перед выдачей пользовательского оружия.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Items.Giving`, перед выдачей оружия</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: типовая отмена отклоняет всю выдачу</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponGivingContext> Giving { get; }

    /// <summary>Вызывается после успешной выдачи пользовательского оружия.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После прикрепления оружия и регистрации в runtime-каталоге</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: оружие уже доступно игроку</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponGivenContext> Given { get; }

    /// <summary>Вызывается на пути получения урона перед применением множителя пользовательского оружия.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>На `TakeDamage.Pre`, после штатного расчёта множителя и до записи урона</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: только O(1); отмена оставляет базовый урон, подписчик обязан не задавать NaN/Infinity/отрицательное значение</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponDamageModifyingContext> DamageModifying { get; }

    /// <summary>Вызывается после применения множителя урона пользовательского оружия.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После записи модифицированного урона в damage info</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: БД, HTTP, логирование каждого попадания запрещены</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponDamageModifiedContext> DamageModified { get; }

    /// <summary>Вызывается перед созданием эффектов попадания пули.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`OnBulletImpactPost`, до создания tracer/muzzle/impact particles</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: отмена отключает пользовательские частицы этого попадания</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponImpactProcessingContext> ImpactProcessing { get; }

    /// <summary>Вызывается после создания эффектов попадания пули.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После создания настроенных частиц попадания</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: событие вызывается даже если для оружия не настроен отдельный тип частицы</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponImpactProcessedContext> ImpactProcessed { get; }

    /// <summary>Вызывается перед списанием денег за боеприпасы.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>По нажатию `E` с активным магазинным оружием, до проверки лимита и оплаты</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: цена и количество изменяемы; значения валидируются</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponAmmoPurchasingContext> AmmoPurchasing { get; }

    /// <summary>Вызывается после списания денег и обновления запаса боеприпасов.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После оплаты и обновления reserve ammo</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: содержит фактически добавленное число патронов с учётом лимита</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponAmmoPurchasedContext> AmmoPurchased { get; }

    /// <summary>Вызывается при ожидаемом отказе покупки боеприпасов.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отсутствии настройки, полном запасе, отмене, неверных значениях или отказе оплаты</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: боеприпасы не изменены</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<WeaponAmmoPurchaseRejectedContext> AmmoPurchaseRejected { get; }
}
