using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database.Entities;

[PrimaryKey(nameof(PrivilegeId), nameof(PermissionId))]
[Index(nameof(PermissionId), Name = "ix_privilege_permissions_permission_id")]
[Table("privilege_permissions", Schema = AdminDbContext.SchemaName)]
internal sealed class PrivilegePermissionEntity
{
    [Column("privilege_id")]
    public int PrivilegeId { get; set; }

    public PrivilegeEntity Privilege { get; set; } = null!;

    [Column("permission_id")]
    public int PermissionId { get; set; }

    public PermissionEntity Permission { get; set; } = null!;
}