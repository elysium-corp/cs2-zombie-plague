using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

/// <summary>
/// Представляет сохранённое назначение привилегии игроку
/// в таблице <c>admin.player_privileges</c>.
/// </summary>
/// <remarks>
/// Пара <see cref="SteamId"/> + <see cref="PrivilegeKey"/> уникальна,
/// поэтому одна и та же привилегия не может быть назначена
/// одному игроку более одного раза.
/// </remarks>
[Index(nameof(SteamId), nameof(PrivilegeKey), Name = "ux_player_privileges_steam_privilege", IsUnique = true)]
[Table("player_privileges", Schema = AdminDbContext.SchemaName)]
internal sealed class PlayerPrivilegeEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("steam_id")]
    public long SteamId { get; set; }

    [MaxLength(128)]
    [Column("privilege_key")]
    public string PrivilegeKey { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}