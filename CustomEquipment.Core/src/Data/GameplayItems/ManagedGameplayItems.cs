using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;

namespace CustomEquipment.Data.GameplayItems;

/// <summary>
/// Описывает встроенный игровой предмет, состояние и порядок которого
/// управляются PostgreSQL-каталогом CustomEquipment.
/// </summary>
public interface IManagedGameplayItem
{
    /// <summary>
    /// Возвращает признак доступности предмета в игровом каталоге.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Возвращает порядок предмета в игровом каталоге.
    /// </summary>
    int SortOrder { get; }
}

/// <summary>
/// Базовая реализация встроенной гранаты с параметрами из runtime-каталога.
/// </summary>
public abstract class ManagedGrenadeItemBase
    : GrenadeItemBase, IShopItem, ILocalizedShopItem, IManagedGameplayItem
{
    private readonly GameplayItemCatalog _catalog;
    private readonly string _implementationKey;

    private protected ManagedGrenadeItemBase(
        GameplayItemCatalog catalog,
        string implementationKey
    )
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _implementationKey = implementationKey;
    }

    private protected GameplayItemDefinition Definition => _catalog.Get(_implementationKey);

    public bool Enabled => Definition.Enabled;

    public int SortOrder => Definition.SortOrder;

    public override string InheritorName => Definition.InheritorName;

    public override AccessFlags AccessFlags => Definition.AccessFlags;

    public override string DisplayName => Definition.DisplayName;

    public string DisplayNameKey => Definition.DisplayNameKey;

    public override string InternalName => Definition.InternalName;

    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public Price Price => new() { Item = Definition.ItemPrice };

    public ItemRarity Rarity => Definition.Rarity;

    public override string Model => Definition.Model;
}

internal abstract class ManagedEquipmentItemBase(
    GameplayItemCatalog catalog,
    string implementationKey
) : EquipmentItemBase, IShopItem, ILocalizedShopItem, IManagedGameplayItem
{
    protected GameplayItemDefinition Definition => catalog.Get(implementationKey);

    public bool Enabled => Definition.Enabled;

    public int SortOrder => Definition.SortOrder;

    public override string InheritorName => Definition.InheritorName;

    public override AccessFlags AccessFlags => Definition.AccessFlags;

    public override string DisplayName => Definition.DisplayName;

    public string DisplayNameKey => Definition.DisplayNameKey;

    public override string InternalName => Definition.InternalName;

    public override string SubclassName => string.Empty;

    public override Slot Slot => Slot.Equipment;

    public override WeaponType WeaponType => WeaponType.Equipment;

    public Price Price => new() { Item = Definition.ItemPrice };

    public ItemRarity Rarity => Definition.Rarity;

    public override string Model => Definition.Model;
}
