using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260924040000_SequenceLaserMineSounds")]
public sealed class SequenceLaserMineSounds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = '{
                "install_sound_duration": 0.882358,
                "charge_sound_duration": 1.109478,
                "ready_sound_duration": 1.287256
            }'::jsonb || settings
            WHERE implementation_key = 'laser_mine';

            UPDATE custom_equipment.gameplay_items AS item
            SET settings = item.settings || COALESCE((
                SELECT jsonb_object_agg(defaults.key, defaults.new_value)
                FROM (VALUES
                    ('arming_duration', '1.0'::jsonb, '2.0'::jsonb),
                    ('install_sound', '"c4.plant"'::jsonb, '"ZombiePlague.lasermine_mechanism_click"'::jsonb),
                    ('charge_sound', '"Weapon_Taser.Charging"'::jsonb, '"ZombiePlague.lasermine_charge_up"'::jsonb),
                    ('ready_sound', '"C4.PlantSoundB"'::jsonb, '"ZombiePlague.lasermine_ready"'::jsonb),
                    ('damage_sound', '"Weapon_Taser.Hit"'::jsonb, '"ZombiePlague.lasermine_electric_zap"'::jsonb),
                    ('destroy_sound', '"BaseGrenade.Explode"'::jsonb, '"ZombiePlague.lasermine_explosion"'::jsonb),
                    ('sound_events_resource', '"soundevents/game_sounds_weapons.vsndevts"'::jsonb,
                        '"soundevents/game_sounds_elysium_weapons.vsndevts"'::jsonb)
                ) AS defaults(key, old_value, new_value)
                WHERE NOT item.settings ? defaults.key OR item.settings -> defaults.key = defaults.old_value
            ), '{}'::jsonb)
            WHERE implementation_key = 'laser_mine';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем выбранные имена звуков и ресурсы при откате; старый код также их поддерживает.
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = settings - ARRAY[
                'install_sound_duration', 'charge_sound_duration', 'ready_sound_duration'
            ]::text[]
            WHERE implementation_key = 'laser_mine';
            """);
    }
}
