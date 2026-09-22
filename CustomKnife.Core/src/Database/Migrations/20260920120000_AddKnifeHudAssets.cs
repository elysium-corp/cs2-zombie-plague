using CustomKnife.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomKnife.src.Database.Migrations;

/// <summary>Раздельные ресурсы списка и карточки ножа, настраиваемые из Flute CMS.</summary>
[DbContext(typeof(CustomKnifeDbContext))]
[Migration("20260920120000_AddKnifeHudAssets")]
public sealed class AddKnifeHudAssets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "hud_icon_path", "hud_preview_path" })
            migrationBuilder.AddColumn<string>(name, "knives", "character varying(512)",
                schema: "custom_knife", maxLength: 512, nullable: true);
        migrationBuilder.AddCheckConstraint("CK_knives_hud_icon_path", "knives",
            "hud_icon_path IS NULL OR hud_icon_path ~ '^panorama/images/([a-z0-9_-]+/)*[a-z0-9_-]+\\.vsvg$'", "custom_knife");
        migrationBuilder.AddCheckConstraint("CK_knives_hud_preview_path", "knives",
            "hud_preview_path IS NULL OR hud_preview_path ~ '^panorama/images/([a-z0-9_-]+/)*[a-z0-9_-]+_png\\.vtex$'", "custom_knife");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_knives_hud_icon_path", "knives", "custom_knife");
        migrationBuilder.DropCheckConstraint("CK_knives_hud_preview_path", "knives", "custom_knife");
        migrationBuilder.DropColumn("hud_icon_path", "knives", "custom_knife");
        migrationBuilder.DropColumn("hud_preview_path", "knives", "custom_knife");
    }
}
