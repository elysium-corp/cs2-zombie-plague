using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260831050000_AddLocalizationColorTags")]
internal sealed class AddLocalizationColorTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE localization.settings
                ADD COLUMN IF NOT EXISTS color_tags JSONB NOT NULL DEFAULT
                    '{"default":"default","accent":"lightblue","warning":"red","success":"green","important":"orange","muted":"gray"}'::jsonb;

            ALTER TABLE localization.settings
                DROP CONSTRAINT IF EXISTS settings_color_tags_object;
            ALTER TABLE localization.settings
                ADD CONSTRAINT settings_color_tags_object
                    CHECK (jsonb_typeof(color_tags) = 'object');

            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE localization.settings
                DROP CONSTRAINT IF EXISTS settings_color_tags_object;
            ALTER TABLE localization.settings
                DROP COLUMN IF EXISTS color_tags;
            """);
    }
}
