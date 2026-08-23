using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Statistics.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateStatisticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "statistics");

            migrationBuilder.CreateTable(
                name: "players",
                schema: "statistics",
                columns: table => new
                {
                    steam_id = table.Column<long>(type: "bigint", nullable: false),
                    last_known_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.steam_id);
                });

            migrationBuilder.CreateTable(
                name: "player_statistics",
                schema: "statistics",
                columns: table => new
                {
                    steam_id = table.Column<long>(type: "bigint", nullable: false),
                    sessions_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    play_time_seconds = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    rounds_played = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    rounds_as_human = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    rounds_as_zombie = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    zombies_killed = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    headshot_zombie_kills = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    infections_made = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    times_infected = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    deaths_as_human = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    deaths_as_zombie = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    damage_to_zombies = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    damage_to_humans = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    survived_rounds = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    human_wins = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    zombie_wins = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    first_zombie_rounds = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_human_rounds = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_human_survivals = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    best_kill_streak = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    best_infection_streak = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_statistics", x => x.steam_id);
                    table.ForeignKey(
                        name: "FK_player_statistics_players_steam_id",
                        column: x => x.steam_id,
                        principalSchema: "statistics",
                        principalTable: "players",
                        principalColumn: "steam_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_statistics",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "players",
                schema: "statistics");
        }
    }
}

