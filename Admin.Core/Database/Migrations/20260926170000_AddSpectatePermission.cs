using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Admin.Core.Database.Migrations;

[DbContext(typeof(AdminDbContext))]
[Migration("20260926170000_AddSpectatePermission")]
internal sealed class AddSpectatePermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO admin.permissions (key, description) VALUES
                ('admin.spectate', 'Переход в наблюдатели и возврат в игру')
            ON CONFLICT (key) DO NOTHING;
            INSERT INTO admin.privilege_permissions (privilege_id, permission_id)
            SELECT privilege.id, permission.id
            FROM admin.privileges privilege CROSS JOIN admin.permissions permission
            WHERE privilege.group_name = 'admin' AND privilege.code = 'owner'
                AND permission.key = 'admin.spectate'
            ON CONFLICT (privilege_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Назначенные права сохраняются, чтобы откат не удалял настройки администратора.
    }
}
