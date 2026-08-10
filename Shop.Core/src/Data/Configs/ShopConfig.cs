using CustomEquipment.Api.Data;

namespace Shop.Core.Data.Configs;

internal sealed class ShopConfig
{
    public bool Enabled { get; set; } = true;

    public ShopCategoryPrices Prices { get; set; } = new();

    public Dictionary<string, ShopItemOverride> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int GetDefaultPrice(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Pistol => Prices.Pistol,
            EquipmentCategory.SubmachineGun => Prices.SubmachineGun,
            EquipmentCategory.Rifle => Prices.Rifle,
            EquipmentCategory.Shotgun => Prices.Shotgun,
            EquipmentCategory.SniperRifle => Prices.SniperRifle,
            EquipmentCategory.MachineGun => Prices.MachineGun,
            EquipmentCategory.Grenade => Prices.Grenade,
            EquipmentCategory.Equipment => Prices.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}

internal sealed class ShopCategoryPrices
{
    public int Pistol { get; set; } = 1_500;

    public int SubmachineGun { get; set; } = 3_500;

    public int Rifle { get; set; } = 5_000;

    public int Shotgun { get; set; } = 4_000;

    public int SniperRifle { get; set; } = 6_500;

    public int MachineGun { get; set; } = 6_000;

    public int Grenade { get; set; } = 1_500;

    public int Equipment { get; set; } = 3_000;
}

internal sealed class ShopItemOverride
{
    public bool Enabled { get; set; } = true;

    public int? Price { get; set; }

    public string? DisplayName { get; set; }
}
