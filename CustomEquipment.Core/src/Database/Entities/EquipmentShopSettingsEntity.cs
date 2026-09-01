using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomEquipment.Database.Entities;

[Table("shop_settings", Schema = CustomEquipmentDbContext.SchemaName)]
internal sealed class EquipmentShopSettingsEntity
{
    [Key, MaxLength(16), Column("shop_type")]
    public string ShopType { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("max_purchases_per_round")]
    public int MaxPurchasesPerRound { get; set; }

    [Column("max_purchases_per_map")]
    public int MaxPurchasesPerMap { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
