using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Economy.Core.Database.Entities;

[Index(nameof(WeaponKey), Name = "ux_economy_weapon_reward_rules_weapon_key", IsUnique = true)]
[Table("weapon_reward_rules", Schema = EconomyDbContext.SchemaName)]
internal sealed class EconomyWeaponRuleEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("weapon_key")]
    [MaxLength(128)]
    public string WeaponKey { get; set; } = string.Empty;

    [Column("damage_bonus_percent", TypeName = "numeric(8,2)")]
    public decimal DamageBonusPercent { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
