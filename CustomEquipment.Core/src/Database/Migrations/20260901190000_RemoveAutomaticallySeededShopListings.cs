using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <summary>
/// Удаляет только позиции, автоматически созданные первой миграцией магазина.
/// </summary>
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260901190000_RemoveAutomaticallySeededShopListings")]
public sealed class RemoveAutomaticallySeededShopListings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PostgreSQL сохраняет CURRENT_TIMESTAMP на начало транзакции: настройки
        // магазина и старые seed-позиции получили одну метку, а записи CMS — более позднюю.
        migrationBuilder.Sql(
            """
            DELETE FROM custom_equipment.shop_listings AS listing
            USING custom_equipment.shop_settings AS settings
            WHERE listing.shop_type = settings.shop_type
              AND listing.created_at = settings.created_at;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Удалённые позиции нельзя безопасно восстановить: после миграции
        // состав обеих витрин полностью контролирует администратор.
    }
}
