using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events.Contexts.Player;

namespace ZombiePlague.Api.Events;

/// <summary>События выбора предпочтительных классов игроков.</summary>
public interface IZombiePlagueClassEvents
{
    /// <summary>Вызывается перед сохранением предпочтительного класса.</summary>
    IHookSubscription<ClassSelectingContext> Selecting { get; }

    /// <summary>Вызывается после сохранения предпочтительного класса в runtime-сессии.</summary>
    IHookSubscription<ClassSelectedContext> Selected { get; }

    /// <summary>Вызывается при ожидаемом отказе сохранения выбора.</summary>
    IHookSubscription<ClassSelectionRejectedContext> SelectionRejected { get; }
}
