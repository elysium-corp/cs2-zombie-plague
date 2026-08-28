using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Api.Events;

/// <summary>
/// События жизненного цикла раундов Zombie Plague.
/// </summary>
public interface IZombiePlagueRoundEvents
{
    /// <summary>Вызывается перед завершением текущего режима и запуском обратного отсчёта.</summary>
    IHookSubscription<RoundPreparingContext> Preparing { get; }

    /// <summary>Вызывается после запуска обратного отсчёта до режима Zombie Plague.</summary>
    IHookSubscription<RoundPreparedContext> Prepared { get; }

    /// <summary>
    /// Вызывается перед запуском выбранного режима раунда. Обработчик может заменить режим или отменить запуск.
    /// </summary>
    IHookSubscription<RoundStartingContext> Starting { get; }

    /// <summary>Вызывается после успешного запуска режима раунда.</summary>
    IHookSubscription<RoundStartedContext> Started { get; }

    /// <summary>Вызывается при ожидаемом отказе в запуске режима.</summary>
    IHookSubscription<RoundStartRejectedContext> StartRejected { get; }

    /// <summary>Вызывается при исключении из пользовательской логики запуска режима.</summary>
    IHookSubscription<RoundStartFailedContext> StartFailed { get; }

    /// <summary>Вызывается перед завершением активного режима.</summary>
    IHookSubscription<RoundEndingContext> Ending { get; }

    /// <summary>Вызывается после завершения активного режима.</summary>
    IHookSubscription<RoundEndedContext> Ended { get; }

    /// <summary>Вызывается перед постановкой режима в очередь на следующий раунд.</summary>
    IHookSubscription<RoundSchedulingContext> Scheduling { get; }

    /// <summary>Вызывается после постановки режима в очередь на следующий раунд.</summary>
    IHookSubscription<RoundScheduledContext> Scheduled { get; }

    /// <summary>Вызывается перед очисткой выбранного следующего режима.</summary>
    IHookSubscription<RoundScheduleClearingContext> ScheduleClearing { get; }

    /// <summary>Вызывается после очистки выбранного следующего режима.</summary>
    IHookSubscription<RoundScheduleClearedContext> ScheduleCleared { get; }
}
