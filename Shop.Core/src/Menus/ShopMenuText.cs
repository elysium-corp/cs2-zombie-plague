using System.Net;
using CustomEquipment.Api.Enums;

namespace Shop.Core.Menus;

internal static class ShopMenuText
{
    public static string Offer(string name, string price, ItemRarity? rarity)
    {
        var color = rarity switch
        {
            ItemRarity.Common => "#B0C3D9",
            ItemRarity.Uncommon => "#5E98D9",
            ItemRarity.Rare => "#4B69FF",
            ItemRarity.Restricted => "#8847FF",
            ItemRarity.Classified => "#D32CE6",
            ItemRarity.Elite => "#EB4B4B",
            ItemRarity.Prototype => "#FF8C00",
            ItemRarity.Legendary => "#E4AE39",
            _ => null
        };
        var text = WebUtility.HtmlEncode(name);
        var label = color is null ? text : $"<font color='{color}'>{text}</font>";
        return $"{label} [{WebUtility.HtmlEncode(price)}]";
    }
}
