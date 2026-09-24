using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260924120000_BoundLaserMineExplosionSound")]
public sealed class BoundLaserMineExplosionSound : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = '{"destroy_sound_duration": 2.0}'::jsonb || settings
            WHERE implementation_key = 'laser_mine';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE custom_equipment.gameplay_items
            SET settings = settings - 'destroy_sound_duration'
            WHERE implementation_key = 'laser_mine';
            """);
    }
}
