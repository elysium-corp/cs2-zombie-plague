using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260908101000_AddNotificationRules")]
internal sealed class AddNotificationRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        ALTER TABLE advertisement.messages DROP CONSTRAINT messages_hud_key_required;
        ALTER TABLE advertisement.messages ADD CONSTRAINT messages_hud_key_required CHECK (display_type = 'chat' OR (hud_localization_key IS NOT NULL AND btrim(hud_localization_key) <> '') OR (banner_template_key IS NOT NULL AND (banner_header_key IS NOT NULL OR banner_title_key IS NOT NULL)));
        ALTER TABLE advertisement.banner_templates
            ADD COLUMN default_header_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            ADD COLUMN default_title_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            ADD COLUMN default_description_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT;
        CREATE TABLE advertisement.notification_rules (
            event_key VARCHAR(191) PRIMARY KEY,
            template_key VARCHAR(64) NOT NULL REFERENCES advertisement.banner_templates(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            header_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            title_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            description_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            settings JSONB NOT NULL CHECK (jsonb_typeof(settings) = 'object' AND octet_length(settings::text) <= 16384),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        CREATE INDEX notification_rules_template_idx ON advertisement.notification_rules(template_key);
        CREATE TABLE advertisement.hud_widgets (
            key VARCHAR(64) PRIMARY KEY,
            settings JSONB NOT NULL CHECK (jsonb_typeof(settings) = 'object' AND octet_length(settings::text) <= 4096)
        );
        INSERT INTO advertisement.banner_templates(key, name, design) VALUES
            ('Notifications.Notice','Уведомления · Notice','{"Variant":"icon","Icon":"info","Width":"small","Size":"small"}'::jsonb),
            ('Notifications.Success','Уведомления · Success','{"Variant":"icon","Icon":"shield","Accent":"mint","Width":"small","Size":"small"}'::jsonb),
            ('Notifications.Warning','Уведомления · Warning','{"Variant":"icon","Icon":"warning","Accent":"gold","Width":"small","Size":"small"}'::jsonb),
            ('Notifications.Round','Уведомления · Round','{"Variant":"icon","Icon":"infection","Accent":"mint","Width":"large","Size":"large","Enter":"slide_down"}'::jsonb),
            ('Notifications.Rating','Уведомления · Rating','{"Variant":"icon","Icon":"trophy","Accent":"gold","Width":"medium","Size":"small"}'::jsonb),
            ('Notifications.Damage','Уведомления · Damage','{"Variant":"text","WidthPixels":400,"Padding":12,"Size":"small","Theme":"glass","Border":"none","Enter":"none","Exit":"fade"}'::jsonb),
            ('Notifications.Countdown','Уведомления · Countdown','{"Variant":"icon","Icon":"clock","Width":"small","Size":"small","Enter":"none","Exit":"none"}'::jsonb) ON CONFLICT (key) DO NOTHING;
        INSERT INTO advertisement.notification_rules(event_key, template_key, description_key, settings) VALUES
            ('Game.Damage.Hit','Notifications.Damage','Notifications.Game.Damage.Hit','{"Enabled":true,"Delivery":"replace","CooldownSeconds":0.1,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":5,"DurationSeconds":1.2,"Priority":100}}'::jsonb),
            ('ZombiePlague.Round.Preparing','Notifications.Countdown','Notifications.ZombiePlague.Round.Preparing','{"Enabled":true,"Delivery":"replace","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":1,"DurationSeconds":1.1,"Priority":100}}'::jsonb),
            ('Game.Round.Started','Notifications.Round','Notifications.Game.Round.Started','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":1,"DurationSeconds":6,"Priority":100}}'::jsonb),
            ('ZombiePlague.Round.Infection.FirstInfected','Notifications.Round','Notifications.ZombiePlague.Round.Infection.FirstInfected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":1,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Round.Nemesis.Selected','Notifications.Round','Notifications.ZombiePlague.Round.Nemesis.Selected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":1,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Round.Survivor.Selected','Notifications.Round','Notifications.ZombiePlague.Round.Survivor.Selected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":1,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('RoundRatingNotify.HumanTop','Notifications.Rating','Notifications.RoundRatingNotify.HumanTop','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":2,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('RoundRatingNotify.ZombieTop','Notifications.Rating','Notifications.RoundRatingNotify.ZombieTop','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":2,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Statistics.PointsGained','Notifications.Notice','Notifications.Statistics.PointsGained','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Statistics.PointsLost','Notifications.Notice','Notifications.Statistics.PointsLost','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Statistics.PointsUnchanged','Notifications.Notice','Notifications.Statistics.PointsUnchanged','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ResetScore.ResetMessage','Notifications.Success','Notifications.ResetScore.ResetMessage','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.ZClass.SelectionSuccess','Notifications.Notice','Notifications.Menu.ZClass.SelectionSuccess','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.HClass.SelectionSuccess','Notifications.Notice','Notifications.Menu.HClass.SelectionSuccess','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.Knife.SelectionSuccess','Notifications.Notice','Notifications.Menu.Knife.SelectionSuccess','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.Knife.PermissionRequired','Notifications.Notice','Notifications.Menu.Knife.PermissionRequired','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.AbilityHud.Disabled','Notifications.Notice','Notifications.Menu.AbilityHud.Disabled','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Menu.AbilityHud.Unavailable','Notifications.Notice','Notifications.Menu.AbilityHud.Unavailable','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.Started','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.Started','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.NotPreparing','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.NotPreparing','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.CannotStart','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.CannotStart','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.Cancelled','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.Cancelled','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.Selection.AutomaticSelected','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.Selection.AutomaticSelected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.Selection.ConditionsPending','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.Selection.ConditionsPending','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('ZombiePlague.Admin.Round.Selection.Selected','Notifications.Notice','Notifications.ZombiePlague.Admin.Round.Selection.Selected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.ProductUnavailable','Notifications.Warning','Notifications.Shop.Errors.ProductUnavailable','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.TeamUnavailable','Notifications.Warning','Notifications.Shop.Errors.TeamUnavailable','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.AccessDenied','Notifications.Warning','Notifications.Shop.Errors.AccessDenied','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.NotEnoughMoney','Notifications.Warning','Notifications.Shop.Errors.NotEnoughMoney','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.RoundLimit','Notifications.Warning','Notifications.Shop.Errors.RoundLimit','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.MapLimit','Notifications.Warning','Notifications.Shop.Errors.MapLimit','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.Cooldown','Notifications.Warning','Notifications.Shop.Errors.Cooldown','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.InvalidPlayer','Notifications.Warning','Notifications.Shop.Errors.InvalidPlayer','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.Cancelled','Notifications.Warning','Notifications.Shop.Errors.Cancelled','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.PaymentRejected','Notifications.Warning','Notifications.Shop.Errors.PaymentRejected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.GrantRejected','Notifications.Warning','Notifications.Shop.Errors.GrantRejected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.RefundFailed','Notifications.Warning','Notifications.Shop.Errors.RefundFailed','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.AmmoNotConfigured','Notifications.Warning','Notifications.Shop.Errors.AmmoNotConfigured','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.AmmoFull','Notifications.Warning','Notifications.Shop.Errors.AmmoFull','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Errors.Unavailable','Notifications.Warning','Notifications.Shop.Errors.Unavailable','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Admin.Reload.Succeeded','Notifications.Notice','Notifications.Shop.Admin.Reload.Succeeded','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Shop.Admin.Reload.Failed','Notifications.Notice','Notifications.Shop.Admin.Reload.Failed','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Localization.Menu.Loading','Notifications.Notice','Notifications.Localization.Menu.Loading','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Localization.Menu.Changed','Notifications.Notice','Notifications.Localization.Menu.Changed','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Localization.Menu.Unavailable','Notifications.Notice','Notifications.Localization.Menu.Unavailable','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Localization.Reload.Result','Notifications.Notice','Notifications.Localization.Reload.Result','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('Advertisement.Reload.Result','Notifications.Notice','Notifications.Advertisement.Reload.Result','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('InfoNotify.Connected','Notifications.Notice','Notifications.InfoNotify.Connected','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('InfoNotify.RoundStart','Notifications.Notice','Notifications.InfoNotify.RoundStart','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('InfoNotify.RoundEnd','Notifications.Notice','Notifications.InfoNotify.RoundEnd','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb),
            ('InfoNotify.Periodic','Notifications.Notice','Notifications.InfoNotify.Periodic','{"Enabled":true,"Delivery":"queue","CooldownSeconds":0,"MaxQueueAgeSeconds":30,"Audience":"all","MinPlayers":0,"Parameters":{},"Options":{"Position":8,"DurationSeconds":5,"Priority":100}}'::jsonb) ON CONFLICT (event_key) DO NOTHING;
        INSERT INTO advertisement.hud_widgets(key, settings) VALUES ('ZombiePlague.Abilities',
            '{"Enabled":true,"Position":"top_left","ScalePercent":100,"ShowNames":true,"AllowPlayerCustomization":true}'::jsonb);
        CREATE TRIGGER notification_rules_configuration AFTER INSERT OR UPDATE OR DELETE
            ON advertisement.notification_rules FOR EACH STATEMENT EXECUTE FUNCTION advertisement.bump_banner_configuration();
        CREATE TRIGGER hud_widgets_configuration AFTER INSERT OR UPDATE OR DELETE
            ON advertisement.hud_widgets FOR EACH STATEMENT EXECUTE FUNCTION advertisement.bump_banner_configuration();
        UPDATE advertisement.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        -- Откат требует сначала назначить описание баннерам с одним заголовком.
        ALTER TABLE advertisement.messages DROP CONSTRAINT messages_hud_key_required;
        ALTER TABLE advertisement.messages ADD CONSTRAINT messages_hud_key_required CHECK (display_type = 'chat' OR (hud_localization_key IS NOT NULL AND btrim(hud_localization_key) <> ''));
        DROP TABLE advertisement.notification_rules;
        DROP TABLE advertisement.hud_widgets;
        ALTER TABLE advertisement.banner_templates DROP COLUMN default_header_key,
            DROP COLUMN default_title_key, DROP COLUMN default_description_key;
        """);
}
