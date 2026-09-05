using System.Collections.Frozen;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Data;

internal static class StandardWeaponCatalog
{
    public const string ProviderKey = "cs2_weapon";

    public static readonly FrozenDictionary<string, gear_slot_t> Weapons =
        new Dictionary<string, gear_slot_t>(StringComparer.Ordinal)
        {
            ["weapon_glock"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_hkp2000"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_usp_silencer"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_elite"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_p250"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_tec9"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_fiveseven"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_cz75a"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_deagle"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_revolver"] = gear_slot_t.GEAR_SLOT_PISTOL,
            ["weapon_mac10"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_mp9"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_mp7"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_mp5sd"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_ump45"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_p90"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_bizon"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_galilar"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_famas"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_ak47"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_m4a1"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_m4a1_silencer"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_aug"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_sg556"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_ssg08"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_awp"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_scar20"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_g3sg1"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_nova"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_xm1014"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_mag7"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_sawedoff"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_m249"] = gear_slot_t.GEAR_SLOT_RIFLE,
            ["weapon_negev"] = gear_slot_t.GEAR_SLOT_RIFLE
        }.ToFrozenDictionary(StringComparer.Ordinal);
}
