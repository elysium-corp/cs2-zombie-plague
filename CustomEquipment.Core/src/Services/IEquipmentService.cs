using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    bool TryGiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    bool CanUseItem(IPlayer player, string internalName);

    TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;

    bool HasItem<TItem>(IPlayer player) where TItem : ItemBase;

    int RemoveItems<TItem>(IPlayer player) where TItem : ItemBase;

    int RemoveInaccessibleItems(IPlayer player);

    TItem? GetItemByEntityIndex<TItem>(uint entityIndex) where TItem : ItemBase;

    WeaponItemBase? GetWeaponByEntityIndex(uint entityIndex);
}
