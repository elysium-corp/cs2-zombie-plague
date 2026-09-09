using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shop.Core.Database.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260909020000_AddShopPlayerPreferences")]
internal sealed class AddShopPlayerPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE shop.player_preferences (
            steam_id BIGINT PRIMARY KEY CHECK (steam_id > 0),
            hud_scale INTEGER NOT NULL DEFAULT 100,
            CONSTRAINT ck_shop_hud_scale CHECK (hud_scale IN (75, 85, 100, 115, 125))
        );
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP TABLE shop.player_preferences;");
}
