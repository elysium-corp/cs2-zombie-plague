using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    void GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    bool CanUseItem(IPlayer player, string internalName);

    TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;
}