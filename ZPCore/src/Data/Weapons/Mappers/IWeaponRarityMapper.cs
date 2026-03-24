using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Utils;

namespace ZPCore.Data.Weapons.Mappers;

internal interface IWeaponRarityMapper
{
    RarityColor MapTo(WeaponRarity rarity);

    WeaponRarity MapTo(RarityColor rarityColor);
}