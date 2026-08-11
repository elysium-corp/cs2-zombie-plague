using CustomEquipment.Api.Enums;

namespace CustomEquipment.Menus.Utils;

internal static class ItemRarityExt
{
    extension(ItemRarity rarity)
    {
        public string ToColor()
        {
            return rarity switch
            {
                ItemRarity.Common => "#B0C3D9",
                ItemRarity.Uncommon => "#5E98D9",
                ItemRarity.Rare => "#4B69FF",
                ItemRarity.Restricted => "#8847FF",
                ItemRarity.Classified => "#D32CE6",
                ItemRarity.Elite => "#EB4B4B",
                ItemRarity.Prototype => "#FF8C00",
                ItemRarity.Legendary => "#E4AE39",
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Rarity color not supported!")
            };
        }
    }
}