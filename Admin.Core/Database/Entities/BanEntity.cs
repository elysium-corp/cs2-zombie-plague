using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

[Index(nameof(SteamId), Name = "ux_bans_steam_id", IsUnique = true)]
[Table("bans", Schema = AdminDbContext.SchemaName)]
internal sealed class BanEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("steam_id")]
    public long SteamId { get; set; }

    [Column("banned_by_steam_id")]
    public long? BannedBySteamId { get; set; }

    [MaxLength(256)]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}