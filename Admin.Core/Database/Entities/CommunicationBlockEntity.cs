using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Admin.Core.Data;

namespace Admin.Core.Database.Entities;

[PrimaryKey(nameof(SteamId), nameof(Kind))]
[Table("communication_blocks", Schema = AdminDbContext.SchemaName)]
internal sealed class CommunicationBlockEntity
{
    [Column("steam_id")]
    public long SteamId { get; set; }

    [Column("kind")]
    public CommunicationKind Kind { get; set; }

    [Column("administrator_steam_id")]
    public long? AdministratorSteamId { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Column("reason"), MaxLength(256)]
    public string Reason { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
