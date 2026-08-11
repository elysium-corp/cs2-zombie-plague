using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;

namespace CustomEquipment.Data.Catalog;

public interface IEquipmentShopCatalog
{
    IDisposable Register(IShopItem item);

    IReadOnlyCollection<IShopItem> GetAll();

    IReadOnlyCollection<IShopItem> GetByWeaponType(WeaponType weaponType);
    
    IReadOnlyCollection<IShopItem> GetByRarity(ItemRarity rarity);
    
    bool TryGet(string internalName, [NotNullWhen(true)] out IShopItem? item);
}