using Common.Hooks.Abstractions;

namespace Economy.Api.Events;

/// <summary>События начисления и списания средств.</summary>
public interface IEconomyTransactionEvents
{
    /// <summary>Вызывается перед изменением баланса; игрока, сумму или саму операцию можно изменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`GiveMoney` и `TrySpendMoney`, после проверки входного аргумента и до доступа к сессии</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: награда за каждый урон проходит здесь; только O(1), сумма и игрок изменяемы</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyTransactionProcessingContext> Processing { get; }

    /// <summary>Вызывается после изменения сессии счёта и игровой проекции баланса.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После атомарного изменения session balance и обновления CS2 money projection</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: persistent-состояние уже изменено; не запускать синхронный I/O</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyTransactionCommittedContext> Committed { get; }

    /// <summary>Вызывается при ожидаемом отказе без изменения баланса.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отмене, отсутствии/незагрузившемся счёте, нехватке средств или лимите</description></item>
    /// <item><term>Частота</term><description>Часто</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Высокий: только наблюдение; повтор операции из обработчика может создать рекурсию</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyTransactionRejectedContext> Rejected { get; }

    /// <summary>Вызывается при ошибке обновления игровой проекции после изменения сессии счёта.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Если обновление CS2 money projection выбросило исключение после изменения session balance</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Критический: возможна временная рассинхронизация; исключение повторно выбрасывается</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyTransactionFailedContext> Failed { get; }
}
