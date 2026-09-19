using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260919191000_AddAmmoHintBanner")]
internal sealed class AddAmmoHintBanner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO advertisement.banner_templates(key, name, design, default_description_key)
        VALUES ('Notifications.Ammo', 'Ammo purchase',
            '{"Variant":"icon","Icon":"key_e","Theme":"glass","Accent":"white","WidthPixels":320,"Padding":12,"Gap":8,"Size":"small","IconSize":32,"Border":"none"}'::jsonb,
            'Notifications.Shop.Ammo.Empty')
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO advertisement.notification_rules(event_key, template_key, description_key, settings)
        VALUES ('Shop.Ammo.Empty', 'Notifications.Ammo', 'Notifications.Shop.Ammo.Empty',
            '{"Enabled":true,"Delivery":"replace","CooldownSeconds":0,"MaxQueueAgeSeconds":1,"Audience":"alive","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":6,"Priority":100}}'::jsonb)
        ON CONFLICT (event_key) DO NOTHING;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Настройки администратора остаются в БД при откате бинарников.
    }
}
