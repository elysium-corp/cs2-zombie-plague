using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.GameplayItems;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments;

/// <summary>
/// Представляет переносимую лазерную мину с параметрами из PostgreSQL-каталога.
/// </summary>
public sealed class LaserMine : EquipmentItemBase, ILocalizedShopItem, IManagedGameplayItem, IHasRarity
{
    private readonly GameplayItemCatalog _catalog;

    /// <summary>
    /// Создаёт мину со встроенными параметрами по умолчанию.
    /// </summary>
    public LaserMine() : this(new GameplayItemCatalog())
    {
    }

    /// <summary>
    /// Создаёт мину с параметрами из указанного runtime-каталога.
    /// </summary>
    /// <param name="catalog">Runtime-каталог параметров встроенных предметов.</param>
    public LaserMine(GameplayItemCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    internal const string ItemDisplayName = "Laser Mine";

    private GameplayItemDefinition Definition => _catalog.Get(GameplayItemKeys.LaserMine);

    internal LaserMineSettings Settings => (LaserMineSettings)Definition.Settings;

    bool IManagedGameplayItem.Enabled => Definition.Enabled;

    int IManagedGameplayItem.SortOrder => Definition.SortOrder;

    public override string InheritorName => Definition.InheritorName;

    public override AccessFlags AccessFlags => Definition.AccessFlags;

    public override string DisplayName => Definition.DisplayName;

    public string DisplayNameKey => Definition.DisplayNameKey;

    public override string InternalName => Definition.InternalName;

    public override string SubclassName => string.Empty;

    public override Slot Slot => Slot.Equipment;

    public override WeaponType WeaponType => WeaponType.Equipment;

    public ItemRarity Rarity => Definition.Rarity;

    public override string Model => Definition.Model;

    public override void OnPurchase(IPlayer owner)
    {
    }
}
