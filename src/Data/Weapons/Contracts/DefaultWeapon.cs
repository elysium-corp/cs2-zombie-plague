using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public class DefaultWeapon(string inheritorName, string displayName, int coast, WeaponType weaponType) : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => inheritorName;
    
    public override string DisplayName => displayName;
    
    public override string InternalName => inheritorName;

    public override WeaponRarity WeaponRarity => WeaponRarity.Serial;
    
    public int Coast => coast;

    public WeaponType WeaponType => weaponType;
    
    public override WeaponSlot Slot { get; }

    public override string Model { get; }
}