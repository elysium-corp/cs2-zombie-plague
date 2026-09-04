using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Core.Database.Entities;

[Table("storefronts", Schema = ShopDbContext.SchemaName)]
internal sealed class ShopStorefrontEntity
{
    [Key, Column("shop_type"), MaxLength(16)]
    public string ShopType { get; set; } = string.Empty;

    [Column("title_key"), MaxLength(191)]
    public string TitleKey { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_mode"), MaxLength(24)]
    public string SortMode { get; set; } = "priority";
}

[Table("categories", Schema = ShopDbContext.SchemaName)]
internal sealed class ShopCategoryEntity
{
    [Key, Column("id")]
    public long Id { get; set; }

    [Column("shop_type"), MaxLength(16)]
    public string ShopType { get; set; } = string.Empty;

    [Column("key"), MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [Column("display_name_key"), MaxLength(191)]
    public string DisplayNameKey { get; set; } = string.Empty;

    [Column("description_key"), MaxLength(191)]
    public string? DescriptionKey { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    public ICollection<ShopOfferEntity> Offers { get; set; } = [];
}

[Table("offers", Schema = ShopDbContext.SchemaName)]
internal sealed class ShopOfferEntity
{
    [Key, Column("id")]
    public long Id { get; set; }

    [Column("shop_type"), MaxLength(16)]
    public string ShopType { get; set; } = string.Empty;

    [Column("provider_key"), MaxLength(64)]
    public string ProviderKey { get; set; } = string.Empty;

    [Column("item_key"), MaxLength(128)]
    public string ItemKey { get; set; } = string.Empty;

    [Column("display_name_key"), MaxLength(191)]
    public string DisplayNameKey { get; set; } = string.Empty;

    [Column("category_id")]
    public long? CategoryId { get; set; }

    [Column("description_key"), MaxLength(191)]
    public string? DescriptionKey { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("ammo_price")]
    public int? AmmoPrice { get; set; }

    [Column("ammo_amount")]
    public int AmmoAmount { get; set; }

    [Column("max_purchases_per_round")]
    public int MaxPurchasesPerRound { get; set; }

    [Column("max_purchases_per_map")]
    public int MaxPurchasesPerMap { get; set; }

    [Column("cooldown_seconds")]
    public int CooldownSeconds { get; set; }

    [Column("access_mode"), MaxLength(16)]
    public string AccessMode { get; set; } = "everyone";

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("settings", TypeName = "jsonb")]
    public string SettingsJson { get; set; } = "{}";

    public ShopCategoryEntity? Category { get; set; }
    public ICollection<ShopOfferPrivilegeEntity> Privileges { get; set; } = [];
}

[Table("offer_privileges", Schema = ShopDbContext.SchemaName)]
internal sealed class ShopOfferPrivilegeEntity
{
    [Column("offer_id")]
    public long OfferId { get; set; }

    [Column("privilege_key"), MaxLength(129)]
    public string PrivilegeKey { get; set; } = string.Empty;

    public ShopOfferEntity Offer { get; set; } = null!;
}

[Table("fallback_state", Schema = ShopDbContext.SchemaName)]
internal sealed class ShopFallbackStateEntity
{
    [Key, Column("id")]
    public short Id { get; set; }

    [Column("dirty")]
    public bool Dirty { get; set; }
}
