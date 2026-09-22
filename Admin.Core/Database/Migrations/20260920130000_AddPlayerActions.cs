using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Admin.Core.Database.Migrations;

[DbContext(typeof(AdminDbContext))]
[Migration("20260920130000_AddPlayerActions")]
internal sealed class AddPlayerActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE admin.communication_blocks (
                steam_id bigint NOT NULL,
                kind integer NOT NULL,
                administrator_steam_id bigint NULL,
                expires_at timestamp with time zone NULL,
                reason character varying(256) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT "PK_communication_blocks" PRIMARY KEY (steam_id, kind)
            );
            INSERT INTO admin.permissions (key, description) VALUES
                ('admin.money', 'Начисление серверных денег'),
                ('admin.noclip', 'Управление noclip игроков'),
                ('admin.grab', 'Захват и перемещение игроков'),
                ('admin.mute', 'Управление голосовыми блокировками'),
                ('admin.gag', 'Управление блокировками текстового чата')
            ON CONFLICT (key) DO NOTHING;
            INSERT INTO admin.privilege_permissions (privilege_id, permission_id)
            SELECT privilege.id, permission.id
            FROM admin.privileges privilege CROSS JOIN admin.permissions permission
            WHERE privilege.group_name = 'admin' AND privilege.code = 'owner'
                AND permission.key IN ('admin.money', 'admin.noclip', 'admin.grab', 'admin.mute', 'admin.gag')
            ON CONFLICT (privilege_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("communication_blocks", "admin");
        // Назначенные права сохраняются, чтобы откат не удалял настройки администратора.
    }
}
