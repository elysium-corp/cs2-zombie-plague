using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

[Index(nameof(Key), Name = "ux_permissions_key", IsUnique = true)]
[Table("permissions", Schema = AdminDbContext.SchemaName)]
internal sealed class PermissionEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [MaxLength(128)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;
    
    [MaxLength(512)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAtUtc { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<PrivilegePermissionEntity> PrivilegePermissions { get; set; } = [];
}