using CustomEquipment.Api.Enums;

namespace CustomEquipment.Api.Data.Contracts;

public interface IShopItem
{
    string InternalName { get; }

    string DisplayName { get; }

    WeaponType WeaponType { get; }
    
    int Price { get; }

    ItemRarity Rarity { get; }
}