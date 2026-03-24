using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Utils;

namespace ZPCore.Data.Weapons.Mappers;

internal class WeaponRarityMapper : IWeaponRarityMapper
{
    public RarityColor MapTo(WeaponRarity rarity)
    {
        return RarityColor.Build(rarity);
    }

    public WeaponRarity MapTo(RarityColor rarityColor)
    {
        return rarityColor.Rarity;
    }

    public WeaponRarity MapTo(string colorTag)
    {
        if (colorTag.Contains(RarityColorTable.Serial))
        {
            return WeaponRarity.Serial;
        }
        
        if (colorTag.Contains(RarityColorTable.Modified))
        {
            return WeaponRarity.Modified;
        }
        
        if (colorTag.Contains(RarityColorTable.Experimental))
        {
            return WeaponRarity.Experimental;
        }
        
        if (colorTag.Contains(RarityColorTable.Prototype))
        {
            return WeaponRarity.Prototype;
        }
        
        if (colorTag.Contains(RarityColorTable.Exclusive))
        {
            return WeaponRarity.Exclusive;
        }
        
        if (colorTag.Contains(RarityColorTable.Secret))
        {
            return WeaponRarity.Secret;
        }
        
        throw new ArgumentOutOfRangeException(nameof(colorTag));
    }
}

internal static class WeaponRarityHelper
{
    private static readonly WeaponRarityMapper Mapper = new();
    
    public static WeaponRarity MapToWeaponRarity(this RarityColor rarityColor)
    {
        return Mapper.MapTo(rarityColor);
    }

    public static RarityColor MapToRarityColor(this WeaponRarity rarity)
    {
        return Mapper.MapTo(rarity);
    }

    public static WeaponRarity MapToWeaponRarity(this string colorTag)
    {
        return Mapper.MapTo(colorTag);
    }
}