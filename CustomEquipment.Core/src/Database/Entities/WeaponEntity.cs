using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomEquipment.Database.Entities;

[Table("weapons", Schema = CustomEquipmentDbContext.SchemaName)]
internal sealed class WeaponEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(128), Column("internal_name")]
    public string InternalName { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(191), Column("display_name_key")]
    public string DisplayNameKey { get; set; } = string.Empty;

    [Required, MaxLength(64), Column("inheritor_name")]
    public string InheritorName { get; set; } = string.Empty;

    [Required, MaxLength(128), Column("subclass_name")]
    public string SubclassName { get; set; } = string.Empty;

    [Required, MaxLength(32), Column("slot")]
    public string Slot { get; set; } = string.Empty;

    [Required, MaxLength(32), Column("weapon_type")]
    public string WeaponType { get; set; } = string.Empty;

    [Column("access_flags")]
    public short AccessFlags { get; set; }

    [Required, MaxLength(32), Column("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [Required, MaxLength(512), Column("model")]
    public string Model { get; set; } = string.Empty;

    [MaxLength(2048), Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("item_price")]
    public int ItemPrice { get; set; }

    [Column("ammo_price")]
    public int? AmmoPrice { get; set; }

    [Column("clip_size")]
    public int? ClipSize { get; set; }

    [Column("reserve_ammo")]
    public int? ReserveAmmo { get; set; }

    [Column("cycle_time_primary")]
    public float? CycleTimePrimary { get; set; }

    [Column("cycle_time_secondary")]
    public float? CycleTimeSecondary { get; set; }

    [Column("deploy_duration")]
    public float? DeployDuration { get; set; }

    [Column("num_bullets")]
    public int? NumBullets { get; set; }

    [Column("penetration")]
    public float? Penetration { get; set; }

    [Column("effective_range")]
    public float? EffectiveRange { get; set; }

    [Column("range_modifier")]
    public float? RangeModifier { get; set; }

    [Column("damage_head")]
    public float? DamageHead { get; set; }

    [Column("damage_chest")]
    public float? DamageChest { get; set; }

    [Column("damage_stomach")]
    public float? DamageStomach { get; set; }

    [Column("damage_left_arm")]
    public float? DamageLeftArm { get; set; }

    [Column("damage_right_arm")]
    public float? DamageRightArm { get; set; }

    [Column("damage_left_leg")]
    public float? DamageLeftLeg { get; set; }

    [Column("damage_right_leg")]
    public float? DamageRightLeg { get; set; }

    [Column("damage_neck")]
    public float? DamageNeck { get; set; }

    [MaxLength(512), Column("particle_tracer")]
    public string? ParticleTracer { get; set; }

    [MaxLength(512), Column("particle_impact")]
    public string? ParticleImpact { get; set; }

    [MaxLength(512), Column("particle_muzzle_flash")]
    public string? ParticleMuzzleFlash { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<WeaponSoundEntity> Sounds { get; set; } = [];
}
