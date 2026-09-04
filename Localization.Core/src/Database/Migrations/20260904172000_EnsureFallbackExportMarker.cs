using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260904172000_EnsureFallbackExportMarker")]
internal sealed class EnsureFallbackExportMarker : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE localization.settings
                ADD COLUMN IF NOT EXISTS fallback_exported_version BIGINT;

            UPDATE localization.settings
            SET fallback_exported_version = GREATEST(
                COALESCE(fallback_exported_version, 0),
                0
            )
            WHERE fallback_exported_version IS NULL
               OR fallback_exported_version < 0;

            ALTER TABLE localization.settings
                ALTER COLUMN fallback_exported_version SET DEFAULT 0;
            ALTER TABLE localization.settings
                ALTER COLUMN fallback_exported_version SET NOT NULL;

            ALTER TABLE localization.settings
                DROP CONSTRAINT IF EXISTS settings_fallback_exported_version_valid;
            ALTER TABLE localization.settings
                ADD CONSTRAINT settings_fallback_exported_version_valid
                CHECK (fallback_exported_version >= 0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Поле принадлежит предыдущей миграции. Откат repair-миграции не должен
        // удалять рабочие данные или снова делать runtime несовместимым со схемой.
    }
}
