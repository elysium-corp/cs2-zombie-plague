using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

[Index(nameof(SteamId), nameof(PrivilegeId), Name = "ux_player_privileges_steam_privilege", IsUnique = true)]
[Table("player_privileges", Schema = AdminDbContext.SchemaName)]
internal sealed class PlayerPrivilegeEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("steam_id")]
    public long SteamId { get; set; }
    
    [Column("privilege_id")]
    public int PrivilegeId { get; set; }

    public PrivilegeEntity Privilege { get; set; } = null!;
    
    [Column("expires_at")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}