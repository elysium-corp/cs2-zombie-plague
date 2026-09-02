using CustomEquipment.Api.Enums;

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

        var settings = new Dictionary<EquipmentShopType, EquipmentShopSettingsDefinition>
        {
            [EquipmentShopType.Human] = new(
                EquipmentShopType.Human,
                "Магазин людей",
                "Equipment.Shop.Human.Title",
                true,
                0,
                0
            ),
            [EquipmentShopType.Zombie] = new(
                EquipmentShopType.Zombie,
                "Магазин зомби",
                "Equipment.Shop.Zombie.Title",
                true,
                0,
                0
            )
        };

        var armor = new EquipmentShopProductDefinition(
            EquipmentShopProductKeys.Armor,
            EquipmentShopItemKeys.Armor,
            "Броня",
            "Equipment.Item.custom_equipment.armor.Name",
            true,
            1_000
        );

        return new EquipmentShopSnapshot(
            settings,
            categories,
            [],
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
                    CategoryDisplayNameKey(weaponType),
                    string.Empty,
                    null,
                    true,
                    (int)weaponType * 10
                ));
            }
        }

        return categories;
    }

    private static string CategoryDisplayNameKey(WeaponType weaponType)
    {
        return $"Menu.Equipment.Category.{weaponType}";
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
