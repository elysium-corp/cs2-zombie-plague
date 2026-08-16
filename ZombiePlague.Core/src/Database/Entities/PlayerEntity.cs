using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Database.Entities;

[Index(
    nameof(SteamId),
    Name = "ux_players_steam_id",
    IsUnique = true
)]
[Table("players", Schema = ZombiePlagueDbContext.SchemaName)]
internal sealed class PlayerEntity
{
    private const int ClassIdMaxLength = 64;

    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("steam_id")]
    public long SteamId { get; set; }

    [Required]
    [MaxLength(ClassIdMaxLength)]
    [Column("zombie_class")]
    public string ZombieClassId { get; set; } = PlayerPreferences.DefaultZombieClassId;

    [Required]
    [MaxLength(ClassIdMaxLength)]
    [Column("human_class")]
    public string HumanClassId { get; set; } = PlayerPreferences.DefaultHumanClassId;

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}