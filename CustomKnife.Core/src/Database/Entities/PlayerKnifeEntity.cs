using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CustomKnife.Database.Entities;

[Table("player_knives", Schema = CustomKnifeDbContext.SchemaName)]
[Index(nameof(SteamId), IsUnique = true)]
internal sealed class PlayerKnifeEntity : ISteamEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("steam_id")]
    public long SteamId { get; set; }

    [Column("knife_id")]
    [MaxLength(64)]
    public string KnifeId { get; set; } = "knife_vengeance";

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}