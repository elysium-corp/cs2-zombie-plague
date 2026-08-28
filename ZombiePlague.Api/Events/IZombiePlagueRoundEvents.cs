using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Api.Events;

/// <summary>
/// События жизненного цикла раундов Zombie Plague.
/// </summary>
public interface IZombiePlagueRoundEvents
{
    /// <summary>Вызывается перед завершением текущего режима и запуском обратного отсчёта.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В начале `RoundManager.Prepare`, до завершения активного режима</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена не запускает подготовку</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundPreparingContext> Preparing { get; }

    /// <summary>Вызывается после запуска обратного отсчёта до режима Zombie Plague.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После назначения людей и запуска таймера обратного отсчёта</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: уведомление о запущенном countdown</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundPreparedContext> Prepared { get; }

    /// <summary>
    /// Вызывается перед запуском выбранного режима раунда. Обработчик может заменить режим или отменить запуск.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`RoundManager.StartRound`, до остановки подготовки и `TryStart`</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: можно отменить или заменить `RoundId`; неизвестная замена игнорируется</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundStartingContext> Starting { get; }

    /// <summary>Вызывается после успешного запуска режима раунда.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После успешного запуска выбранного режима или fallback `infection`</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: `Round` содержит фактически запущенный режим</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundStartedContext> Started { get; }

    /// <summary>Вызывается при ожидаемом отказе в запуске режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>На ветках `NotPreparing`, `CannotStart` и отмены `Starting`</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: только аудит ожидаемого отказа</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundStartRejectedContext> StartRejected { get; }

    /// <summary>Вызывается при исключении из пользовательской логики запуска режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>В `TryStartRoundInternal`, когда `Round.TryStart()` выбрасывает исключение</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: исключение будет выброшено повторно; обработчик не должен скрывать восстановление</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundStartFailedContext> StartFailed { get; }

    /// <summary>Вызывается перед завершением активного режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`RoundManager.End`, до остановки подготовки и `Round.End()`</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена оставляет активный режим</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundEndingContext> Ending { get; }

    /// <summary>Вызывается после завершения активного режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Round.End()` и очистки `CurrentRound`</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: состояние режима уже очищено</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundEndedContext> Ended { get; }

    /// <summary>Вызывается перед постановкой режима в очередь на следующий раунд.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`SelectNextRound`, до записи `NextRound`</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена не меняет текущую очередь</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundSchedulingContext> Scheduling { get; }

    /// <summary>Вызывается после постановки режима в очередь на следующий раунд.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После записи `NextRound`</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: уведомление об очереди</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundScheduledContext> Scheduled { get; }

    /// <summary>Вызывается перед очисткой выбранного следующего режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`ClearNextRound`, если режим был выбран, до очистки</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена сохраняет выбранный режим</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundScheduleClearingContext> ScheduleClearing { get; }

    /// <summary>Вызывается после очистки выбранного следующего режима.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После очистки `NextRound`</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: содержит удалённый режим</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<RoundScheduleClearedContext> ScheduleCleared { get; }
}
