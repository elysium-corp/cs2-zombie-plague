using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admin.Core.Database.Migrations;

/// <summary>
/// Материализует текущие разрешения роли владельца перед удалением
/// специальной runtime-логики, автоматически выдававшей ей все разрешения.
/// </summary>
[DbContext(typeof(AdminDbContext))]
[Migration("20260829111500_MaterializeOwnerPermissions")]
public sealed class MaterializeOwnerPermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO admin.privilege_permissions (privilege_id, permission_id)
            SELECT privilege.id, permission.id
            FROM admin.privileges AS privilege
            CROSS JOIN admin.permissions AS permission
            WHERE privilege.group_name = 'admin'
              AND privilege.code = 'owner'
            ON CONFLICT (privilege_id, permission_id) DO NOTHING;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Миграция намеренно не удаляет связи: невозможно надёжно отличить
        // ранее назначенные вручную разрешения owner от добавленных миграцией.
    }
}
