using System.Collections.Frozen;
using System.Text.Json;
using CustomHud.Api;

namespace Advertisement.Core.Application;

internal static class NotificationCatalog
{
    internal static readonly JsonElement Document = Load();
    internal static readonly FrozenDictionary<string, HudBannerTemplate> Templates = new Dictionary<string, HudBannerTemplate>
    {
        ["Notifications.Notice"] = new() { Variant = "icon", Icon = "info", Width = "small", Size = "small" },
        ["Notifications.Success"] = new() { Variant = "icon", Icon = "shield", Accent = "mint", Width = "small", Size = "small" },
        ["Notifications.Warning"] = new() { Variant = "icon", Icon = "warning", Accent = "gold", Width = "small", Size = "small" },
        ["Notifications.Round"] = new() { Variant = "icon", Icon = "infection", Accent = "mint", Width = "large", Size = "large", Enter = "slide_down" },
        ["Notifications.Rating"] = new() { Variant = "icon", Icon = "trophy", Accent = "gold", Width = "medium", Size = "small" },
        ["Notifications.Damage"] = new() { Variant = "text", WidthPixels = 400, Padding = 12, Size = "small", Theme = "glass", Border = "none", Enter = "none", Exit = "fade" },
        ["Notifications.Countdown"] = new() { Variant = "icon", Icon = "clock", Width = "small", Size = "small", Enter = "none", Exit = "none" }
    }.ToFrozenDictionary(StringComparer.Ordinal);
    internal static readonly FrozenDictionary<string, BannerNotificationRule> Defaults = Document.GetProperty("events").EnumerateArray()
        .Select(item => new BannerNotificationRule
        {
            EventKey = item.GetProperty("key").GetString()!,
            Template = Templates[item.GetProperty("default_template").GetString()!],
            Content = new() { Description = item.GetProperty("localization_key").GetString() },
            Options = new() { Position = Position(item.GetProperty("position").GetString()!),
                DurationSeconds = item.GetProperty("duration").GetDouble(), Priority = 100 },
            Delivery = item.GetProperty("delivery").GetString()!, CooldownSeconds = item.GetProperty("cooldown").GetDouble()
        }).ToFrozenDictionary(item => item.EventKey, StringComparer.Ordinal);
    internal static readonly FrozenDictionary<string, string[]> Aliases = Document.GetProperty("parameters").EnumerateArray()
        .ToFrozenDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("aliases").EnumerateArray()
            .Select(alias => alias.GetString()!).ToArray(), StringComparer.OrdinalIgnoreCase);

    private static JsonElement Load()
    {
        using var stream = typeof(IBannerNotificationApi).Assembly.GetManifestResourceStream("CustomHud.Api.Resources.notification-catalog.json")
            ?? throw new InvalidDataException("Отсутствует каталог игровых уведомлений");
        using var json = JsonDocument.Parse(stream);
        return json.RootElement.Clone();
    }

    internal static HudPosition Position(string value) => value switch
    {
        "top_left" => HudPosition.TopLeft, "top_center" => HudPosition.TopCenter, "top_right" => HudPosition.TopRight,
        "middle_left" => HudPosition.MiddleLeft, "center" => HudPosition.Center, "middle_right" => HudPosition.MiddleRight,
        "bottom_left" => HudPosition.BottomLeft, "bottom_center" => HudPosition.BottomCenter, "bottom_right" => HudPosition.BottomRight,
        _ => throw new ArgumentException("Неизвестная позиция уведомления")
    };

    internal static object? Scalar(object? value) => value is JsonElement json ? json.ValueKind switch
    {
        JsonValueKind.String => json.GetString(), JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.Number when json.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => json.GetDouble(), _ => null
    } : value;

    internal static void Validate(BannerNotificationRule rule)
    {
        if (!Defaults.ContainsKey(rule.EventKey) || !Enum.IsDefined(rule.Options.Position)
            || !double.IsFinite(rule.Options.DurationSeconds) || rule.Options.DurationSeconds is < .5 or > 60
            || rule.Options.Priority is < 0 or > 1000 || rule.Delivery is not ("replace" or "queue")
            || !double.IsFinite(rule.CooldownSeconds) || rule.CooldownSeconds is < 0 or > 300
            || !double.IsFinite(rule.MaxQueueAgeSeconds) || rule.MaxQueueAgeSeconds is < 1 or > 120
            || rule.Audience is not ("all" or "alive" or "dead" or "humans" or "zombies" or "spectators")
            || rule.MinPlayers is < 0 or > 128 || rule.Parameters.Count > 64)
            throw new InvalidDataException("Некорректная настройка уведомления: " + rule.EventKey);
        var fields = HudBannerFields.Get(rule.Template);
        foreach (var (field, key) in new[] { ("Header", rule.Content.Header), ("Title", rule.Content.Title), ("Description", rule.Content.Description) })
            if (fields.Contains(field) ? string.IsNullOrWhiteSpace(key) : !string.IsNullOrEmpty(key))
                throw new InvalidDataException("Заполните включённые блоки уведомления: " + rule.EventKey);
    }
}
