using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomEquipment.Database.Entities;

[Table("weapon_sound_files", Schema = CustomEquipmentDbContext.SchemaName)]
internal sealed class WeaponSoundFileEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("sound_id")]
    public long SoundId { get; set; }

    [Column("track")]
    public int Track { get; set; }

    [Required, MaxLength(512), Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    public WeaponSoundEntity Sound { get; set; } = null!;
}
