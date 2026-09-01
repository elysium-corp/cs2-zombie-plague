using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomKnife.src.Database.Migrations;

[DbContext(typeof(CustomKnifeDbContext))]
[Migration("20260901220500_AddKnifeLocalizationAndImage")]
public sealed class AddKnifeLocalizationAndImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "display_name_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            type: "character varying(191)",
            maxLength: 191,
            nullable: true
        );
        migrationBuilder.AddColumn<string>(
            name: "description_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            type: "character varying(191)",
            maxLength: 191,
            nullable: true
        );
        migrationBuilder.AddColumn<string>(
            name: "image_url",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true
        );

        migrationBuilder.Sql(
            """
            UPDATE custom_knife.knives
            SET display_name_key =
                    'CustomKnife.' ||
                    regexp_replace(internal_name, '[^A-Za-z0-9_.-]', '_', 'g') ||
                    '.Name',
                description_key =
                    'CustomKnife.' ||
                    regexp_replace(internal_name, '[^A-Za-z0-9_.-]', '_', 'g') ||
                    '.Description';
            """
        );

        migrationBuilder.AlterColumn<string>(
            name: "display_name_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            type: "character varying(191)",
            maxLength: 191,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(191)",
            oldMaxLength: 191,
            oldNullable: true
        );
        migrationBuilder.AlterColumn<string>(
            name: "description_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            type: "character varying(191)",
            maxLength: 191,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(191)",
            oldMaxLength: 191,
            oldNullable: true
        );

        migrationBuilder.AddCheckConstraint(
            name: "CK_knives_localization_keys",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$' AND description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_knives_image_url",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            sql: "image_url IS NULL OR image_url ~ '^https://[^[:space:]]+$' OR image_url ~ '^assets/uploads/elysium-equipments/items/[a-f0-9]{40}\\.(jpg|jpeg|png|webp|avif)$'"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_knives_localization_keys",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives"
        );
        migrationBuilder.DropCheckConstraint(
            name: "CK_knives_image_url",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives"
        );
        migrationBuilder.DropColumn(
            name: "display_name_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives"
        );
        migrationBuilder.DropColumn(
            name: "description_key",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives"
        );
        migrationBuilder.DropColumn(
            name: "image_url",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives"
        );
    }
}
