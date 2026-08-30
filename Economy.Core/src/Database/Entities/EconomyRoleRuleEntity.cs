using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Economy.Core.Database.Entities;

[Index(nameof(PrivilegeKey), Name = "ux_economy_role_rules_privilege_key", IsUnique = true)]
[Table("role_rules", Schema = EconomyDbContext.SchemaName)]
internal sealed class EconomyRoleRuleEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("privilege_key")]
    [MaxLength(129)]
    public string PrivilegeKey { get; set; } = string.Empty;

    [Column("max_money")]
    public int MaxMoney { get; set; }

    [Column("reward_bonus_percent", TypeName = "numeric(8,2)")]
    public decimal RewardBonusPercent { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
