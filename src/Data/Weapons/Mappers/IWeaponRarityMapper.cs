using CS2ZombiePlague.Data.Weapons.Enums;
using CS2ZombiePlague.Data.Weapons.Utils;

namespace CS2ZombiePlague.Data.Weapons.Mappers;

public interface IWeaponRarityMapper
{
    RarityColor MapTo(WeaponRarity rarity);

    WeaponRarity MapTo(RarityColor rarityColor);
}