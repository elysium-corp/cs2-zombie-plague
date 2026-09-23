using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260923120000_AddLaserMineSounds")]
public sealed class AddLaserMineSounds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Новые значения добавляются только при отсутствии ключей; настройки сервера сохраняются.
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = '{
                "arming_duration": 1.0,
                "install_sound": "c4.plant",
                "charge_sound": "Weapon_Taser.Charging",
                "ready_sound": "C4.PlantSoundB",
                "damage_sound": "Weapon_Taser.Hit",
                "destroy_sound": "BaseGrenade.Explode",
                "sound_volume": 0.7,
                "damage_sound_interval": 0.3,
                "sound_events_resource": "soundevents/game_sounds_weapons.vsndevts"
            }'::jsonb || settings
            WHERE implementation_key = 'laser_mine';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = settings - ARRAY[
                'arming_duration', 'install_sound', 'charge_sound', 'ready_sound',
                'damage_sound', 'destroy_sound', 'sound_volume', 'damage_sound_interval',
                'sound_events_resource'
            ]::text[]
            WHERE implementation_key = 'laser_mine';
            """);
    }
}
