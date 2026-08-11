using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Economy.Core.Database.Entities;

[Index(
    nameof(SteamId),
    Name = "ux_accounts_steam_id",
    IsUnique = true
)]
[Table("accounts", Schema = "economy")]
internal sealed class Account
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("steam_id")]
    public long SteamId { get; set; }
    
    [Column("balance")]
    public int Balance { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}