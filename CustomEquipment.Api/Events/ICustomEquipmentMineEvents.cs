using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События размещения лазерных мин.
/// </summary>
public interface ICustomEquipmentMineEvents
{
    /// <summary>Вызывается перед размещением подготовленной лазерной мины; размещение можно отменить.</summary>
    IHookSubscription<MinePlacingContext> Placing { get; }

    /// <summary>Вызывается после успешного размещения лазерной мины.</summary>
    IHookSubscription<MinePlacedContext> Placed { get; }

    /// <summary>Вызывается при ожидаемом отказе размещения лазерной мины.</summary>
    IHookSubscription<MinePlacementRejectedContext> PlacementRejected { get; }
}
