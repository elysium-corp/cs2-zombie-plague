using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Admin.Core.Database.Migrations;

[DbContext(typeof(AdminDbContext))]
[Migration("20260908120000_AddRoleColors")]
internal sealed class AddRoleColors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE admin.privileges
                ADD COLUMN chat_color character varying(16) NOT NULL DEFAULT 'default',
                ADD COLUMN hud_color character varying(7) NOT NULL DEFAULT '#ffffff',
                ADD COLUMN color_priority integer NOT NULL DEFAULT 0;
            UPDATE admin.privileges AS p
            SET chat_color = 'red', hud_color = '#ff4040', color_priority = 100
            WHERE p.group_name = 'admin' OR EXISTS (
                SELECT 1 FROM admin.privilege_permissions AS link
                JOIN admin.permissions AS permission ON permission.id = link.permission_id
                WHERE link.privilege_id = p.id AND (permission.key LIKE 'admin.%' OR permission.key = '*')
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE admin.privileges DROP COLUMN chat_color, DROP COLUMN hud_color, DROP COLUMN color_priority;
            """);
    }
}
