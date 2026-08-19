using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    IEnumerable<ItemBase> GetAllItems();

    IEnumerable<WeaponItemBase> GetAllWeapons();

    IEnumerable<GrenadeItemBase> GetAllGrenades();
    
    bool CanUseItem(IPlayer player, ItemBase item);

    bool CanUseItem(IPlayer player, string name);

    TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : WeaponItemBase;

    WeaponItemBase? GiveWeapon(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    GrenadeItemBase? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : GrenadeItemBase;

    TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;

    TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase;
}