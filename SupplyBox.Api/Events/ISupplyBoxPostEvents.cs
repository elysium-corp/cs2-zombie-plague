using Common.Hooks;
using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

namespace SupplyBox.Api.Events;

/// <summary>
/// События после выполнения операций с ящиками снабжения.
/// </summary>
public interface ISupplyBoxPostEvents
{
    /// <summary>Возникает после сброса ящика.</summary>
    event HookHandler<SupplyBoxDropPostContext> DropEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<SupplyBoxDropPostContext> Drop { get; }

    /// <summary>Возникает после подбора ящика.</summary>
    event HookHandler<SupplyBoxPickUpPostContext> PickUpEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<SupplyBoxPickUpPostContext> PickUp { get; }
}
