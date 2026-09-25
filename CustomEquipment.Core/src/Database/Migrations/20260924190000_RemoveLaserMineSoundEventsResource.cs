using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260924190000_RemoveLaserMineSoundEventsResource")]
public sealed class RemoveLaserMineSoundEventsResource : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Настройка удалена из LaserMineSettings; строгий JSON каталога не принимает неизвестные поля.
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = settings - 'sound_events_resource'
            WHERE implementation_key = 'laser_mine';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = '{"sound_events_resource": "soundevents/game_sounds_elysium_weapons.vsndevts"}'::jsonb || settings
            WHERE implementation_key = 'laser_mine';
            """);
    }
}
