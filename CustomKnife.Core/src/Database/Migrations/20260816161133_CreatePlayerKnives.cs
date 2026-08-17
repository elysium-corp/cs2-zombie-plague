using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomKnife.src.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreatePlayerKnives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "custom_knife");

            migrationBuilder.CreateTable(
                name: "player_knives",
                schema: "custom_knife",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    steam_id = table.Column<long>(type: "bigint", nullable: false),
                    knife_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_knives", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_knives_steam_id",
                schema: "custom_knife",
                table: "player_knives",
                column: "steam_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_knives",
                schema: "custom_knife");
        }
    }
}
