using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

[Index(nameof(Group), nameof(Code), Name = "ux_privileges_group_code", IsUnique = true)]
[Table("privileges", Schema = AdminDbContext.SchemaName)]
internal sealed class PrivilegeEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [MaxLength(64)]
    [Column("group_name")]
    public string Group { get; set; } = string.Empty;
    
    [MaxLength(64)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(128)]
    [Column("display_name")]
    public string? DisplayName { get; set; }
    
    [MaxLength(512)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<PrivilegePermissionEntity> PrivilegePermissions { get; set; } = [];

    public ICollection<PlayerPrivilegeEntity> PlayerPrivileges { get; set; } = [];
}