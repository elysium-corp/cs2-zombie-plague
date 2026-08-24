using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomEquipment.Database.Entities;

[Table("weapon_sounds", Schema = CustomEquipmentDbContext.SchemaName)]
internal sealed class WeaponSoundEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("weapon_id")]
    public long WeaponId { get; set; }

    [Required, MaxLength(64), Column("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [Required, MaxLength(256), Column("event_name")]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(256), Column("replaces_event_name")]
    public string? ReplacesEventName { get; set; }

    [Required, MaxLength(64), Column("sound_type")]
    public string SoundType { get; set; } = "csgo_mega";

    [Column("volume")]
    public float Volume { get; set; }

    [Column("pitch")]
    public float Pitch { get; set; }

    [Required, MaxLength(64), Column("mix_group")]
    public string MixGroup { get; set; } = "Weapons";

    [Column("preload_vsnds")]
    public bool PreloadVsnds { get; set; }

    [Column("extra_properties", TypeName = "jsonb")]
    public string? ExtraPropertiesJson { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public WeaponEntity Weapon { get; set; } = null!;

    public ICollection<WeaponSoundFileEntity> Files { get; set; } = [];
}
