using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;

namespace CustomEquipment.Api.Data.Contracts;

public interface IShopItem
{
    string InternalName { get; }

    string DisplayName { get; }

    WeaponType WeaponType { get; }
    
    Price Price { get; }

    ItemRarity Rarity { get; }
}