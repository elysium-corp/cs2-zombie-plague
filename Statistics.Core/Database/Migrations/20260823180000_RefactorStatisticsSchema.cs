using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Statistics.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStatisticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "deaths",
                schema: "statistics",
                table: "player_statistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "points",
                schema: "statistics",
                table: "player_statistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(
                """
                UPDATE statistics.player_statistics
                SET deaths = deaths_as_human + deaths_as_zombie;
                """);

            migrationBuilder.DropColumn(name: "sessions_count", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "rounds_played", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "rounds_as_human", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "rounds_as_zombie", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "headshot_zombie_kills", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "deaths_as_human", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "deaths_as_zombie", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "damage_to_zombies", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "damage_to_humans", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "survived_rounds", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "first_zombie_rounds", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "last_human_rounds", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "last_human_survivals", schema: "statistics", table: "player_statistics");

            migrationBuilder.AddCheckConstraint(
                name: "CK_player_statistics_points_non_negative",
                schema: "statistics",
                table: "player_statistics",
                sql: "points >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_player_statistics_points_non_negative",
                schema: "statistics",
                table: "player_statistics");

            migrationBuilder.AddColumn<long>(name: "sessions_count", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "rounds_played", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "rounds_as_human", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "rounds_as_zombie", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "headshot_zombie_kills", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "deaths_as_human", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "deaths_as_zombie", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "damage_to_zombies", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "damage_to_humans", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "survived_rounds", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "first_zombie_rounds", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "last_human_rounds", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<long>(name: "last_human_survivals", schema: "statistics", table: "player_statistics", type: "bigint", nullable: false, defaultValue: 0L);

            migrationBuilder.Sql(
                """
                UPDATE statistics.player_statistics
                SET deaths_as_human = deaths;
                """);

            migrationBuilder.DropColumn(name: "deaths", schema: "statistics", table: "player_statistics");
            migrationBuilder.DropColumn(name: "points", schema: "statistics", table: "player_statistics");
        }
    }
}
