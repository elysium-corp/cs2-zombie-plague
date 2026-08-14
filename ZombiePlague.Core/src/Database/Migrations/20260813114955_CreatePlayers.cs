using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZombiePlague.Core.Database.Migrations;

public partial class CreatePlayers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "zombie_plague"
        );

        migrationBuilder.CreateTable(
            name: "players",
            schema: "zombie_plague",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                    ),
                steam_id = table.Column<long>(type: "bigint", nullable: false),
                zombie_class = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false,
                    defaultValue: "zombie_cleric"
                ),
                human_class = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false,
                    defaultValue: "human_mercenary"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_players", player => player.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "ux_players_steam_id",
            schema: "zombie_plague",
            table: "players",
            column: "steam_id",
            unique: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "players",
            schema: "zombie_plague"
        );
    }
}
