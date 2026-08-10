using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    public IEnumerable<BaseItem> GetAllItems();

    public IEnumerable<WeaponBase> GetAllWeapons();

    public IEnumerable<BaseGrenade> GetAllGrenades();
    
    public WeaponBase? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponBase;

    public BaseGrenade? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : BaseGrenade;

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : BaseItem;

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponBase;
}