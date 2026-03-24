using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Utils;

internal record RarityColor
{
    public WeaponRarity Rarity { get; private init; }
    
    public required string Color { get; set; }

    public static RarityColor Build(WeaponRarity rarity)
    {
        var color = rarity switch
        {
            WeaponRarity.Serial => RarityColorTable.Serial,
            WeaponRarity.Modified => RarityColorTable.Modified,
            WeaponRarity.Experimental => RarityColorTable.Experimental,
            WeaponRarity.Prototype => RarityColorTable.Prototype,
            WeaponRarity.Exclusive => RarityColorTable.Exclusive,
            WeaponRarity.Secret => RarityColorTable.Secret,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null)
        };
        return new RarityColor { Rarity = rarity, Color = color };
    }
}

public static class RarityColorTable
{
    public const string Serial = "#5E98D9";
    public const string Modified = "#4B69FF";
    public const string Experimental = "#8847FF";
    public const string Prototype = "#D32CE6";
    public const string Exclusive = "#EB4B4B";
    public const string Secret = "#E4AE39";
}