using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database.Entities;

[Table("shop_categories", Schema = CustomEquipmentDbContext.SchemaName)]
[Index(nameof(ShopType), nameof(Key), IsUnique = true)]
internal sealed class EquipmentShopCategoryEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(16), Column("shop_type")]
    public string ShopType { get; set; } = string.Empty;

    [Required, MaxLength(64), Column("key")]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<EquipmentShopListingEntity> Listings { get; set; } = [];
}
