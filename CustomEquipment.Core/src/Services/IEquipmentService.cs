using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    public IEnumerable<ItemBase> GetAllItems();

    public IEnumerable<WeaponItemBase> GetAllWeapons();

    public IEnumerable<ItemBaseGrenade> GetAllGrenades();
    
    public WeaponItemBase? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase;

    public ItemBaseGrenade? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : ItemBaseGrenade;

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase;

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase;
}