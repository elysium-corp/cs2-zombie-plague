using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shop.Core.Database.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260909121000_AddShopHudAppearance")]
internal sealed class AddShopHudAppearance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "hud_settings", schema: "shop", table: "storefronts", type: "jsonb", nullable: false,
        defaultValueSql: "'{}'::jsonb");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "hud_settings", schema: "shop", table: "storefronts");
}
