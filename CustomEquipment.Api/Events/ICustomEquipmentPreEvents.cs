using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api.Events;

/// <summary>
/// Отменяемые события до выполнения операций пользовательского снаряжения.
/// </summary>
public interface ICustomEquipmentPreEvents
{
    /// <summary>Возникает перед покупкой предмета.</summary>
    event HookHandler<ItemBuyPreContext> ItemBuyEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<ItemBuyPreContext> ItemBuy { get; }

    /// <summary>Возникает перед выдачей любого предмета.</summary>
    event HookHandler<ItemGivePreContext> ItemGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<ItemGivePreContext> ItemGive { get; }

    /// <summary>Возникает перед выдачей оружия.</summary>
    event HookHandler<WeaponGivePreContext> WeaponGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<WeaponGivePreContext> WeaponGive { get; }

    /// <summary>Возникает перед выдачей гранаты.</summary>
    event HookHandler<GrenadeGivePreContext> GrenadeGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeGivePreContext> GrenadeGive { get; }

    /// <summary>Возникает перед обработкой броска гранаты.</summary>
    event HookHandler<GrenadeThrowPreContext> GrenadeThrowEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeThrowPreContext> GrenadeThrow { get; }

    /// <summary>Возникает перед пользовательской детонацией гранаты.</summary>
    event HookHandler<GrenadeDetonatePreContext> GrenadeDetonateEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeDetonatePreContext> GrenadeDetonate { get; }

    /// <summary>Возникает перед размещением лазерной мины.</summary>
    event HookHandler<MinePlacePreContext> MinePlaceEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<MinePlacePreContext> MinePlace { get; }
}
