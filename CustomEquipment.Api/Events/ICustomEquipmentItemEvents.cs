using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События покупок и общей выдачи пользовательских предметов.
/// </summary>
public interface ICustomEquipmentItemEvents
{
    /// <summary>Legacy-событие старого магазина; сохранено только для бинарной совместимости.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.Purchasing`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: можно отменить или подменить игрока/предмет</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemPurchasingContext> Purchasing { get; }

    /// <summary>Legacy-событие старого магазина; сохранено только для бинарной совместимости.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Не вызывается начиная с CustomEquipment.Core 0.6.0; оплатой управляет Shop.Core</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: деньги уже списаны, предмет ещё не выдан</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemPaymentCommittedContext> PaymentCommitted { get; }

    /// <summary>Legacy-событие старого магазина; сохранено только для бинарной совместимости.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.Purchased`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: не считать это гарантией завершённой асинхронной выдачи; для этого есть `Items.Given`</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemPurchasedContext> Purchased { get; }

    /// <summary>Legacy-событие старого магазина; сохранено только для бинарной совместимости.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.PurchaseRejected`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: баланс не менялся либо уже запущен возврат</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemPurchaseRejectedContext> PurchaseRejected { get; }

    /// <summary>Legacy-событие старого магазина; сохранено только для бинарной совместимости.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Не вызывается начиная с CustomEquipment.Core 0.6.0; возвратом управляет Shop.Core</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: обработчики экономики способны изменить/отменить возврат; проверять её `Transactions`</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemPaymentRefundedContext> PaymentRefunded { get; }

    /// <summary>Вызывается непосредственно перед выдачей любого пользовательского предмета.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`EquipmentService.TryGiveItem`, после создания экземпляра и до проверки конкретного типа</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена/подмена меняет выдачу</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemGivingContext> Giving { get; }

    /// <summary>Вызывается после завершения выдачи любого пользовательского предмета.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Из callback `ItemGiver` после фактического прикрепления/применения предмета</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Высокий: для гранаты вызывается на следующем world update, для других типов может быть синхронным</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemGivenContext> Given { get; }

    /// <summary>Вызывается при ожидаемом отказе выдачи предмета.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>На ожидаемых ветках отказа `TryGiveItem`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: выдача не была поставлена в очередь</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemGiveRejectedContext> GiveRejected { get; }

    /// <summary>Вызывается при технической ошибке создания или постановки выдачи.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При исключении создания предмета или постановки выдачи</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: исключение будет выброшено повторно</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ItemGiveFailedContext> GiveFailed { get; }
}
