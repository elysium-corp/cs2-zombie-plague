using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Combat;

namespace ZombiePlague.Api.Events;

/// <summary>События боевой механики Zombie Plague.</summary>
public interface IZombiePlagueCombatEvents
{
    /// <summary>Вызывается перед применением рассчитанного отбрасывания.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`KnockbackService.TryApplyKnockback`, после расчёта скорости и до `Teleport`</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: только O(1); неверная `Velocity` ломает движение/физику</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<KnockbackApplyingContext> KnockbackApplying { get; }

    /// <summary>Вызывается после применения отбрасывания.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Teleport` и постановки таймера восстановления скорости</description></item>
    /// <item><term>Частота</term><description>Горячий путь</description></item>
    /// <item><term>Нагрузка</term><description>Высокая</description></item>
    /// <item><term>Риск</term><description>Критический: нельзя выполнять I/O или тяжёлую телеметрию синхронно</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<KnockbackAppliedContext> KnockbackApplied { get; }
}
