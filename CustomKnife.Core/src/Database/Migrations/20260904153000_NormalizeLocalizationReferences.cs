using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomKnife.Database.Migrations;

[DbContext(typeof(CustomKnifeDbContext))]
[Migration("20260904153000_NormalizeLocalizationReferences")]
internal sealed class NormalizeLocalizationReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION custom_knife.canonicalize_localization_key(source TEXT)
            RETURNS TEXT
            LANGUAGE SQL
            IMMUTABLE
            STRICT
            PARALLEL SAFE
            AS $function$
                SELECT string_agg(
                    CASE
                        WHEN part.ordinality = 1 AND lower(part.value) = 'tags' THEN 'Tag'
                        ELSE upper(left(part.value, 1)) || substr(part.value, 2)
                    END,
                    '.' ORDER BY part.ordinality
                )
                FROM unnest(regexp_split_to_array(btrim(source), '[._[:space:]-]+'))
                    WITH ORDINALITY AS part(value, ordinality)
                WHERE part.value <> ''
            $function$;

            ALTER TABLE custom_knife.knives
                DROP CONSTRAINT IF EXISTS "CK_knives_localization_keys";

            UPDATE custom_knife.knives
            SET display_name_key = custom_knife.canonicalize_localization_key(display_name_key),
                description_key = custom_knife.canonicalize_localization_key(description_key),
                updated_at = NOW();

            ALTER TABLE custom_knife.knives
                ADD CONSTRAINT "CK_knives_localization_keys"
                CHECK (
                    display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'
                    AND description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'
                );

            DROP FUNCTION custom_knife.canonicalize_localization_key(TEXT);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE custom_knife.knives
                DROP CONSTRAINT IF EXISTS "CK_knives_localization_keys";
            ALTER TABLE custom_knife.knives
                ADD CONSTRAINT "CK_knives_localization_keys"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$' AND description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            """);
    }
}
