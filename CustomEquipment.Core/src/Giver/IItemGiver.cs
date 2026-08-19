using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Giver;

internal interface IItemGiver
{
    void GiveItem(IPlayer player, ItemBase item, GiveAction action, Action<ItemBase> onCompleted);
}