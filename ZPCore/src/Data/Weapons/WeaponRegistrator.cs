using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Grenades;
using ZPCore.Data.Weapons.Guns;

namespace ZPCore.Data.Weapons;

internal sealed class WeaponRegistrator : IWeaponRegistrator
{
    private readonly Dictionary<WeaponType, List<BaseWeapon>> _weapons = [];

    private readonly HashSet<BaseWeapon> _customWeapons =
    [
        // Weapons 
        new X3(), new ReactorLeak(), new Omega(),
        new Frostbyte(), new Elite(), new Blackline(),
        
        // Grenades
        new BarrierNade(), new FrostNade(), new FireNade(), new JumpNade()
    ];

    private readonly HashSet<BaseWeapon> _defaultWeapons =
    [
        // Pistols
        new DefaultWeapon("weapon_glock", "Glock-18", 200, WeaponType.Pistol),
        new DefaultWeapon("weapon_usp_silencer", "USP-S", 200, WeaponType.Pistol),
        new DefaultWeapon("weapon_hkp2000", "P2000", 200, WeaponType.Pistol),
        new DefaultWeapon("weapon_elite", "Dual Berettas", 400, WeaponType.Pistol),
        new DefaultWeapon("weapon_p250", "P250", 300, WeaponType.Pistol),
        new DefaultWeapon("weapon_fiveseven", "Five-SeveN", 500, WeaponType.Pistol),
        new DefaultWeapon("weapon_tec9", "Tec-9", 500, WeaponType.Pistol),
        new DefaultWeapon("weapon_cz75a", "CZ75-Auto", 500, WeaponType.Pistol),
        new DefaultWeapon("weapon_deagle", "Desert Eagle", 700, WeaponType.Pistol),
        new DefaultWeapon("weapon_revolver", "R8 Revolver", 600, WeaponType.Pistol),

        // Submachine Guns
        new DefaultWeapon("weapon_mac10", "MAC-10", 1050, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_mp9", "MP9", 1250, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_mp7", "MP7", 1500, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_mp5sd", "MP5-SD", 1500, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_ump45", "UMP-45", 1200, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_p90", "P90", 2350, WeaponType.SubmachineGun),
        new DefaultWeapon("weapon_bizon", "PP-Bizon", 1400, WeaponType.SubmachineGun),

        // Rifles
        new DefaultWeapon("weapon_ak47", "AK-47", 2700, WeaponType.Rifle),
        new DefaultWeapon("weapon_m4a1", "M4A4", 3100, WeaponType.Rifle),
        new DefaultWeapon("weapon_m4a1_silencer", "M4A1-S", 2900, WeaponType.Rifle),
        new DefaultWeapon("weapon_famas", "FAMAS", 2050, WeaponType.Rifle),
        new DefaultWeapon("weapon_galilar", "Galil AR", 1800, WeaponType.Rifle),
        new DefaultWeapon("weapon_aug", "AUG", 3300, WeaponType.Rifle),
        new DefaultWeapon("weapon_sg556", "SG 553", 3000, WeaponType.Rifle),

        // Shotguns
        new DefaultWeapon("weapon_nova", "Nova", 1050, WeaponType.Shotgun),
        new DefaultWeapon("weapon_xm1014", "XM1014", 2000, WeaponType.Shotgun),
        new DefaultWeapon("weapon_mag7", "MAG-7", 1300, WeaponType.Shotgun),
        new DefaultWeapon("weapon_sawedoff", "Sawed-Off", 1100, WeaponType.Shotgun),

        // Sniper Rifles
        new DefaultWeapon("weapon_ssg08", "SSG 08", 1700, WeaponType.SniperRifle),
        new DefaultWeapon("weapon_awp", "AWP", 4750, WeaponType.SniperRifle),
        new DefaultWeapon("weapon_scar20", "SCAR-20", 5000, WeaponType.SniperRifle),
        new DefaultWeapon("weapon_g3sg1", "G3SG1", 5000, WeaponType.SniperRifle),

        // Machine Guns
        new DefaultWeapon("weapon_m249", "M249", 5200, WeaponType.MachineGun),
        new DefaultWeapon("weapon_negev", "Negev", 1700, WeaponType.MachineGun),
    ];

    public void Registration()
    {
        _weapons.Clear();

        RegisterCollection(_defaultWeapons);
        RegisterCollection(_customWeapons);
    }

    public List<IWeaponPurchasable>? GetWeaponsByType(WeaponType type)
    {
        _weapons.TryGetValue(type, out var weapons);
        var weaponsCopy = weapons?.Cast<IWeaponPurchasable>().ToList();
        return weaponsCopy;
    }

    public List<BaseWeapon> GetAllWeapons()
    {
        return _weapons
            .SelectMany(x => x.Value)
            .Distinct()
            .ToList();
    }

    private void RegisterCollection(IEnumerable<BaseWeapon> collection)
    {
        foreach (var weapon in collection)
        {
            var weaponAsPurchasable = (IWeaponPurchasable)weapon;
            if (!_weapons.TryGetValue(weaponAsPurchasable.WeaponType, out var list))
            {
                list = [];
                _weapons[weaponAsPurchasable.WeaponType] = list;
            }

            list.Add(weapon);
        }
    }
}