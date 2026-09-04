using Common.Hooks.Abstractions;

namespace Shop.Api.Events;

/// <summary>Публичные события магазина.</summary>
public interface IShopEvents
{
    /// <summary>Вызывается перед покупкой товара.</summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term>Когда</term><description>После первой проверки оффера, до повторной проверки, списания и выдачи</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена, подмена игрока или цены меняет покупку; все значения будут повторно проверены</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ShopPurchasingContext> Purchasing { get; }

    /// <summary>Вызывается после успешной оплаты и выдачи товара.</summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term>Когда</term><description>После списания, принятой выдачи и записи лимитов оффера</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: для гранаты фактическое прикрепление сущности может завершиться на следующем world update</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ShopPurchasedContext> Purchased { get; }

    /// <summary>Вызывается при ожидаемом отказе покупки.</summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term>Когда</term><description>При недоступности, отмене, отказе списания/выдачи или ошибке возврата</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: событие предназначено для UI, аудита и телеметрии; повторять покупку из обработчика нельзя</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ShopPurchaseRejectedContext> PurchaseRejected { get; }

    /// <summary>Вызывается после успешной покупки патронов кнопкой E.</summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term>Когда</term><description>После списания цены патронов и успешного увеличения резерва активного оружия</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: баланс и reserve ammo уже изменены; обработчик не должен повторять операцию</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ShopAmmoPurchasedContext> AmmoPurchased { get; }
}
