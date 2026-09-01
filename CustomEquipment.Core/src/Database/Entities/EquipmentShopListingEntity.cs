using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database.Entities;

[Table("shop_listings", Schema = CustomEquipmentDbContext.SchemaName)]
[Index(nameof(ShopType), nameof(ItemInternalName), IsUnique = true)]
internal sealed class EquipmentShopListingEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(16), Column("shop_type")]
    public string ShopType { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("item_internal_name")]
    public string ItemInternalName { get; set; } = string.Empty;

    [Column("category_id")]
    public long CategoryId { get; set; }

    [Required, MaxLength(1024), Column("description")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(191), Column("description_key")]
    public string? DescriptionKey { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("max_purchases_per_round")]
    public int MaxPurchasesPerRound { get; set; }

    [Column("max_purchases_per_map")]
    public int MaxPurchasesPerMap { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Required, Column("settings", TypeName = "jsonb")]
    public string SettingsJson { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public EquipmentShopCategoryEntity Category { get; set; } = null!;
}
