using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomEquipment.Database.Entities;

[Table("shop_products", Schema = CustomEquipmentDbContext.SchemaName)]
internal sealed class EquipmentShopProductEntity
{
    [Key, MaxLength(64), Column("implementation_key")]
    public string ImplementationKey { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
