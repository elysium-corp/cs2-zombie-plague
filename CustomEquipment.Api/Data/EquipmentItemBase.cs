using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Data;

public abstract class EquipmentItemBase : ItemBase, IEquipment
{
    public abstract string InheritorName { get; }

    public abstract WeaponType WeaponType { get; }

    public abstract void OnPurchase(IPlayer owner);
}