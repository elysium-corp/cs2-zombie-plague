using Common.Hooks.Abstractions;

namespace Economy.Api.Events;

/// <summary>События жизненного цикла денежных счетов.</summary>
public interface IEconomyAccountEvents
{
    /// <summary>Вызывается после создания runtime-сессии со стартовым балансом.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После создания runtime-сессии и применения стартового баланса игроку, до фоновой загрузки</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: загруженный баланс ещё неизвестен</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountInitializedContext> Initialized { get; }

    /// <summary>Вызывается после объединения runtime-изменений с данными БД.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В фоновой очереди после загрузки/создания записи и merge локальной дельты</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: не игровой поток; game projection существующего счёта обновляется позднее через scheduler</description></item>
    /// <item><term>Поток</term><description>Фоновая очередь БД</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountLoadedContext> Loaded { get; }

    /// <summary>Вызывается при технической ошибке загрузки счёта.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В фоновой очереди при исключении загрузки</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: не игровой поток; исключение повторно передаётся tracker-у задач</description></item>
    /// <item><term>Поток</term><description>Фоновая очередь БД</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountLoadFailedContext> LoadFailed { get; }

    /// <summary>Вызывается после удаления runtime-сессии непосредственно перед постановкой сохранения в очередь.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После удаления runtime-сессии при disconnect/unload, перед постановкой сохранения</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: счёт больше недоступен через API, сохранение ещё может завершиться ошибкой</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountRemovedContext> Removed { get; }

    /// <summary>Вызывается после успешного сохранения dirty snapshot в БД.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В фоновой очереди после записи dirty snapshot и `MarkSaved`</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: не игровой поток; не хранить ссылки на игроков/entity</description></item>
    /// <item><term>Поток</term><description>Фоновая очередь БД</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountSavedContext> Saved { get; }

    /// <summary>Вызывается при технической ошибке сохранения счёта.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В фоновой очереди при исключении сохранения</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: runtime-сессия уже удалена; требуется внешняя диагностика/повтор инфраструктуры</description></item>
    /// <item><term>Поток</term><description>Фоновая очередь БД</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<EconomyAccountSaveFailedContext> SaveFailed { get; }
}
