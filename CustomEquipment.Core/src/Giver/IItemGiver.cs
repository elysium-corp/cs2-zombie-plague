using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Giver;

internal interface IItemGiver
{
    BaseItem? GiveItem(IPlayer player, string itemId, GiveAction action = GiveAction.Drop);

    TItem? GiveItem<TItem>(IPlayer player, GiveAction action = GiveAction.Drop) where TItem : class, IItem;

    TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : BaseWeapon;

    TGrenade? GiveGrenade<TGrenade>(IPlayer player, GiveAction action = GiveAction.Drop)
        where TGrenade : BaseGrenade;
}
