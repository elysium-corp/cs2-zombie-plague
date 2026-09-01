using CustomEquipment.Api.Enums;
using CustomEquipment.Data.GameplayItems;

namespace CustomEquipment.Data.Shop;

internal static class EquipmentShopProductKeys
{
    public const string Armor = "armor";
}

internal static class EquipmentShopItemKeys
{
    public const string Armor = "custom_equipment:armor";
}

internal static class EquipmentShopDefaults
{
    private static readonly WeaponType[] CategoryWeaponTypes = Enum.GetValues<WeaponType>();

    public static EquipmentShopSnapshot CreateSnapshot()
    {
        var categories = CreateCategories();
        var listings = CreateGameplayItemListings(categories);
        var armorCategory = categories.Single(category =>
            category.ShopType == EquipmentShopType.Human &&
            category.Key == CategoryKey(WeaponType.Equipment)
        );

        listings.Add(new EquipmentShopListingDefinition(
            1_000,
            EquipmentShopType.Human,
            EquipmentShopItemKeys.Armor,
            armorCategory.Id,
            "Добавляет указанное количество брони, но не выше 100",
            100,
            1,
            0,
            true,
            1_000,
            new ArmorEquipmentShopListingSettings(50)
        ));

        var settings = new Dictionary<EquipmentShopType, EquipmentShopSettingsDefinition>
        {
            [EquipmentShopType.Human] = new(
                EquipmentShopType.Human,
                "Магазин людей",
                true,
                0,
                0
            ),
            [EquipmentShopType.Zombie] = new(
                EquipmentShopType.Zombie,
                "Магазин зомби",
                true,
                0,
                0
            )
        };

        var armor = new EquipmentShopProductDefinition(
            EquipmentShopProductKeys.Armor,
            EquipmentShopItemKeys.Armor,
            "Броня",
            true,
            1_000
        );

        return new EquipmentShopSnapshot(
            settings,
            categories,
            listings,
            [],
            new Dictionary<string, EquipmentShopProductDefinition>(StringComparer.Ordinal)
            {
                [armor.ImplementationKey] = armor
            }
        );
    }

    public static string CategoryKey(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.SubmachineGun => "submachine_gun",
            WeaponType.SniperRifle => "sniper_rifle",
            WeaponType.MachineGun => "machine_gun",
            _ => weaponType.ToString().ToLowerInvariant()
        };
    }

    private static List<EquipmentShopCategoryDefinition> CreateCategories()
    {
        var categories = new List<EquipmentShopCategoryDefinition>();
        long id = 1;

        foreach (var shopType in Enum.GetValues<EquipmentShopType>())
        {
            foreach (var weaponType in CategoryWeaponTypes)
            {
                categories.Add(new EquipmentShopCategoryDefinition(
                    id++,
                    shopType,
                    CategoryKey(weaponType),
                    CategoryDisplayName(weaponType),
                    string.Empty,
                    true,
                    (int)weaponType * 10
                ));
            }
        }

        return categories;
    }

    private static List<EquipmentShopListingDefinition> CreateGameplayItemListings(
        IReadOnlyCollection<EquipmentShopCategoryDefinition> categories
    )
    {
        var listings = new List<EquipmentShopListingDefinition>();
        long id = 1;

        foreach (var item in GameplayItemDefaults.All)
        {
            var categoryKey = item.ImplementationKey == GameplayItemKeys.LaserMine
                ? CategoryKey(WeaponType.Equipment)
                : CategoryKey(WeaponType.Grenade);

            foreach (var shopType in Enum.GetValues<EquipmentShopType>())
            {
                var accessFlag = shopType == EquipmentShopType.Human
                    ? AccessFlags.Human
                    : AccessFlags.Zombie;

                if ((item.AccessFlags & accessFlag) == 0)
                {
                    continue;
                }

                var category = categories.Single(candidate =>
                    candidate.ShopType == shopType &&
                    candidate.Key == categoryKey
                );

                listings.Add(new EquipmentShopListingDefinition(
                    id++,
                    shopType,
                    item.InternalName,
                    category.Id,
                    string.Empty,
                    item.ItemPrice,
                    0,
                    0,
                    true,
                    item.SortOrder,
                    EmptyEquipmentShopListingSettings.Instance
                ));
            }
        }

        return listings;
    }

    private static string CategoryDisplayName(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Pistol => "Пистолеты",
            WeaponType.SubmachineGun => "Пистолеты-пулемёты",
            WeaponType.Rifle => "Винтовки",
            WeaponType.Shotgun => "Дробовики",
            WeaponType.SniperRifle => "Снайперские винтовки",
            WeaponType.MachineGun => "Пулемёты",
            WeaponType.Grenade => "Гранаты",
            WeaponType.Equipment => "Экипировка",
            _ => weaponType.ToString()
        };
    }
}
