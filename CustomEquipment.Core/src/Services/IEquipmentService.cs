using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    bool GiveItem(IPlayer player, string itemId, GiveAction action = GiveAction.Drop);

    List<BaseWeapon> GetAllWeapons();

    List<BaseGrenade> GetAllGrenades();
    
    BaseWeapon? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon;

    BaseGrenade? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : BaseGrenade;

    TItem? GetActiveItem<TItem>(IPlayer player) where TItem : BaseItem;

    TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon;
}
