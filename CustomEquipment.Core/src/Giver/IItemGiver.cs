using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Giver;

internal interface IItemGiver
{
    public TItem? GiveItem<TItem>(IPlayer player, GiveAction action = GiveAction.Drop) where TItem : class, IItem;

    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : BaseWeapon;

    public TGrenade? GiveGrenade<TGrenade>(IPlayer player, GiveAction action = GiveAction.Drop)
        where TGrenade : BaseGrenade;
}