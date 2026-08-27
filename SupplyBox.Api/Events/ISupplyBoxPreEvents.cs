using Common.Hooks;
using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

namespace SupplyBox.Api.Events;

/// <summary>
/// Отменяемые события до выполнения операций с ящиками снабжения.
/// </summary>
public interface ISupplyBoxPreEvents
{
    /// <summary>Возникает перед сбросом ящика.</summary>
    event HookHandler<SupplyBoxDropPreContext> DropEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<SupplyBoxDropPreContext> Drop { get; }

    /// <summary>Возникает перед подбором ящика.</summary>
    event HookHandler<SupplyBoxPickUpPreContext> PickUpEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<SupplyBoxPickUpPreContext> PickUp { get; }
}
