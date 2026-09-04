using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    bool TryGiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    bool CanUseItem(IPlayer player, string internalName);

    TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;

    bool CanRefillActiveWeapon(IPlayer player, string expectedInternalName);

    bool TryRefillActiveWeapon(
        IPlayer player,
        string expectedInternalName,
        int amount,
        out AmmoRefillResult result);

    bool HasItem<TItem>(IPlayer player) where TItem : ItemBase;

    int RemoveItems<TItem>(IPlayer player) where TItem : ItemBase;

    int RemoveInaccessibleItems(IPlayer player);

    TItem? GetItemByEntityIndex<TItem>(uint entityIndex) where TItem : ItemBase;

    WeaponItemBase? GetWeaponByEntityIndex(uint entityIndex);
}
