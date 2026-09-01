using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database.Entities;

[Table("gameplay_items", Schema = CustomEquipmentDbContext.SchemaName)]
[Index(nameof(ImplementationKey), IsUnique = true)]
[Index(nameof(InternalName), IsUnique = true)]
internal sealed class GameplayItemEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(64), Column("implementation_key")]
    public string ImplementationKey { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(64), Column("inheritor_name")]
    public string InheritorName { get; set; } = string.Empty;

    [Column("access_flags")]
    public short AccessFlags { get; set; }

    [Required, MaxLength(32), Column("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("model")]
    public string Model { get; set; } = string.Empty;

    [Column("item_price")]
    public int ItemPrice { get; set; }

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
}
