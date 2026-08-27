using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События после выполнения операций пользовательского снаряжения.
/// </summary>
public interface ICustomEquipmentPostEvents
{
    /// <summary>Возникает после успешной покупки предмета.</summary>
    event HookHandler<ItemBuyPostContext> ItemBuyEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<ItemBuyPostContext> ItemBuy { get; }

    /// <summary>Возникает после выдачи любого предмета.</summary>
    event HookHandler<ItemGivePostContext> ItemGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<ItemGivePostContext> ItemGive { get; }

    /// <summary>Возникает после выдачи оружия.</summary>
    event HookHandler<WeaponGivePostContext> WeaponGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<WeaponGivePostContext> WeaponGive { get; }

    /// <summary>Возникает после выдачи гранаты.</summary>
    event HookHandler<GrenadeGivePostContext> GrenadeGiveEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeGivePostContext> GrenadeGive { get; }

    /// <summary>Возникает после обработки броска гранаты.</summary>
    event HookHandler<GrenadeThrowPostContext> GrenadeThrowEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeThrowPostContext> GrenadeThrow { get; }

    /// <summary>Возникает после пользовательской детонации гранаты.</summary>
    event HookHandler<GrenadeDetonatePostContext> GrenadeDetonateEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<GrenadeDetonatePostContext> GrenadeDetonate { get; }

    /// <summary>Возникает после размещения лазерной мины.</summary>
    event HookHandler<MinePlacePostContext> MinePlaceEvent;
    /// <summary>Подписка с поддержкой приоритета.</summary>
    IHookSubscription<MinePlacePostContext> MinePlace { get; }
}
