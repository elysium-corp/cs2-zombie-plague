using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomKnife.Database.Entities;

[Table("knives", Schema = CustomKnifeDbContext.SchemaName)]
[Index(nameof(InternalName), IsUnique = true)]
internal sealed class KnifeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(64), Column("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(191), Column("display_name_key")]
    public string DisplayNameKey { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(191), Column("description_key")]
    public string DescriptionKey { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("model")]
    public string Model { get; set; } = string.Empty;

    [MaxLength(2048), Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("speed")]
    public float Speed { get; set; }

    [Column("knockback_recoil")]
    public float KnockbackRecoil { get; set; }

    [Column("knockback_pick_distance")]
    public float KnockbackPickDistance { get; set; }

    [Column("gravity")]
    public int Gravity { get; set; }

    [Column("damage_multiplier")]
    public float DamageMultiplier { get; set; }

    [MaxLength(128), Column("required_permission")]
    public string? RequiredPermission { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
