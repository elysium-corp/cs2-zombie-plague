using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database.Entities;

[Table("shop_role_limits", Schema = CustomEquipmentDbContext.SchemaName)]
[Index(nameof(ShopType), nameof(PrivilegeKey), IsUnique = true)]
internal sealed class EquipmentShopRoleLimitEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(16), Column("shop_type")]
    public string ShopType { get; set; } = string.Empty;

    [Required, MaxLength(129), Column("privilege_key")]
    public string PrivilegeKey { get; set; } = string.Empty;

    [Column("max_purchases_per_round")]
    public int MaxPurchasesPerRound { get; set; }

    [Column("max_purchases_per_map")]
    public int MaxPurchasesPerMap { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
