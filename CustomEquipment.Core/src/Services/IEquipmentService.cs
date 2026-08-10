using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    public IEnumerable<ItemBase> GetAllItems();

    public IEnumerable<WeaponItemBase> GetAllWeapons();

    public IEnumerable<GrenadeItemBase> GetAllGrenades();
    
    public WeaponItemBase? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase;

    public GrenadeItemBase? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : GrenadeItemBase;

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase;
}