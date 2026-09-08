using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advertisement.Core.Database.Entities;

[Table("notification_rules", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class NotificationRuleEntity
{
    [Key, MaxLength(191), Column("event_key")]
    public string EventKey { get; set; } = "";
    [MaxLength(64), Column("template_key")]
    public string TemplateKey { get; set; } = "";
    public BannerTemplateEntity Template { get; set; } = null!;
    [MaxLength(191), Column("header_key")]
    public string? HeaderKey { get; set; }
    [MaxLength(191), Column("title_key")]
    public string? TitleKey { get; set; }
    [MaxLength(191), Column("description_key")]
    public string? DescriptionKey { get; set; }
    [Column("settings", TypeName = "jsonb")]
    public string SettingsJson { get; set; } = "{}";
}

[Table("hud_widgets", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class HudWidgetEntity
{
    [Key, MaxLength(64), Column("key")]
    public string Key { get; set; } = "";
    [Column("settings", TypeName = "jsonb")]
    public string SettingsJson { get; set; } = "{}";
}
