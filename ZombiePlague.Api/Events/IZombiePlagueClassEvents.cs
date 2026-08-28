using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Player;

namespace ZombiePlague.Api.Events;

/// <summary>События выбора предпочтительных классов игроков.</summary>
public interface IZombiePlagueClassEvents
{
    /// <summary>Вызывается перед сохранением предпочтительного класса.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`PlayerRepository.SetZClassId/SetHClassId`, до доступа к persistent-сессии</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: можно отменить или заменить игрока/идентификатор класса; существование класса проверяет вызывающий UI</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ClassSelectingContext> Selecting { get; }

    /// <summary>Вызывается после сохранения предпочтительного класса в runtime-сессии.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После записи предпочтения в runtime-сессию</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: сохранение в БД выполняется позднее при lifecycle-сохранении</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ClassSelectedContext> Selected { get; }

    /// <summary>Вызывается при ожидаемом отказе сохранения выбора.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отмене, пустом идентификаторе или отсутствии сессии</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: runtime-предпочтение не изменено</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<ClassSelectionRejectedContext> SelectionRejected { get; }
}
