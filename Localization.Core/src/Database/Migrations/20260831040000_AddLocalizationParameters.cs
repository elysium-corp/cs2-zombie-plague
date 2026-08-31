using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260831040000_AddLocalizationParameters")]
internal sealed class AddLocalizationParameters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE localization.entries
                ADD COLUMN IF NOT EXISTS parameters JSONB NOT NULL DEFAULT '[]'::jsonb;

            ALTER TABLE localization.entries
                DROP CONSTRAINT IF EXISTS entries_parameters_array;
            ALTER TABLE localization.entries
                ADD CONSTRAINT entries_parameters_array
                    CHECK (jsonb_typeof(parameters) = 'array');

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
            ALTER TABLE localization.entries
                DROP CONSTRAINT IF EXISTS entries_parameters_array;
            ALTER TABLE localization.entries
                DROP COLUMN IF EXISTS parameters;
            """);
    }
}
