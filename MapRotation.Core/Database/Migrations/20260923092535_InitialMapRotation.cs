using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MapRotation.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialMapRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "map_rotation");

            migrationBuilder.CreateTable(
                name: "map_history",
                schema: "map_rotation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<long>(type: "bigint", nullable: true),
                    map_name = table.Column<string>(type: "text", nullable: false),
                    workshop_id = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "maps",
                schema: "map_rotation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    map_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    workshop_id = table.Column<long>(type: "bigint", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    weight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    cooldown_maps = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    allow_nomination = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_vote = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_auto_rotation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "runtime_state",
                schema: "map_rotation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    checkpoint = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runtime_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "map_rotation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    map_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    scheduled_vote_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    scheduled_vote_before_seconds = table.Column<int>(type: "integer", nullable: false),
                    vote_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    vote_options_count = table.Column<int>(type: "integer", nullable: false),
                    nomination_slots = table.Column<int>(type: "integer", nullable: false),
                    rtv_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    rtv_ratio = table.Column<double>(type: "double precision", nullable: false),
                    rtv_min_votes = table.Column<int>(type: "integer", nullable: false),
                    rtv_min_players = table.Column<int>(type: "integer", nullable: false),
                    rtv_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    rtv_change_mode = table.Column<string>(type: "text", nullable: false),
                    rtv_change_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    nominations_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    exclude_bots = table.Column<bool>(type: "boolean", nullable: false),
                    include_spectators = table.Column<bool>(type: "boolean", nullable: false),
                    allow_same_map = table.Column<bool>(type: "boolean", nullable: false),
                    recent_maps_excluded = table.Column<int>(type: "integer", nullable: false),
                    final_round_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    fallback_map_id = table.Column<long>(type: "bigint", nullable: true),
                    refresh_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    configuration_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vote_sessions",
                schema: "map_rotation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    current_map = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    winner_map_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vote_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vote_options",
                schema: "map_rotation",
                columns: table => new
                {
                    vote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<long>(type: "bigint", nullable: false),
                    map_name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    votes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vote_options", x => new { x.vote_id, x.map_id });
                    table.ForeignKey(
                        name: "FK_vote_options_vote_sessions_vote_id",
                        column: x => x.vote_id,
                        principalSchema: "map_rotation",
                        principalTable: "vote_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "map_rotation",
                table: "settings",
                columns: new[] { "id", "allow_same_map", "configuration_version", "exclude_bots", "fallback_map_id", "final_round_timeout_seconds", "include_spectators", "map_duration_seconds", "nomination_slots", "nominations_enabled", "recent_maps_excluded", "refresh_interval_seconds", "rtv_change_delay_seconds", "rtv_change_mode", "rtv_delay_seconds", "rtv_enabled", "rtv_min_players", "rtv_min_votes", "rtv_ratio", "scheduled_vote_before_seconds", "scheduled_vote_enabled", "vote_duration_seconds", "vote_options_count" },
                values: new object[] { 1, false, 1L, true, null, 600, true, 2700, 3, true, 3, 15, 0, "end_of_round", 300, true, 4, 3, 0.59999999999999998, 300, true, 20, 6 });

            migrationBuilder.CreateIndex(
                name: "IX_map_history_ended_at",
                schema: "map_rotation",
                table: "map_history",
                column: "ended_at");

            migrationBuilder.CreateIndex(
                name: "IX_maps_key",
                schema: "map_rotation",
                table: "maps",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maps_map_name",
                schema: "map_rotation",
                table: "maps",
                column: "map_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vote_sessions_started_at",
                schema: "map_rotation",
                table: "vote_sessions",
                column: "started_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "map_history",
                schema: "map_rotation");

            migrationBuilder.DropTable(
                name: "maps",
                schema: "map_rotation");

            migrationBuilder.DropTable(
                name: "runtime_state",
                schema: "map_rotation");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "map_rotation");

            migrationBuilder.DropTable(
                name: "vote_options",
                schema: "map_rotation");

            migrationBuilder.DropTable(
                name: "vote_sessions",
                schema: "map_rotation");
        }
    }
}
