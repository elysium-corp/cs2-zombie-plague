using CustomEquipment.Data.Equipments.Contracts;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal interface IEquipmentService
{
    void Initialize();

    public List<BaseWeapon> GetAllWeapons();

    public List<BaseGrenade> GetAllGrenades();
    
    public BaseWeapon? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon;

    public BaseGrenade? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : BaseGrenade;

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : BaseItem;

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon;
}