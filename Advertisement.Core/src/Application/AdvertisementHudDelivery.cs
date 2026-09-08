using Advertisement.Core.Data;
using CustomHud.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Application;

internal enum AdvertisementDeliveryMode { Chat, Hud, ChatAndHud }

internal sealed class AdvertisementHudRule
{
    public AdvertisementDeliveryMode Mode { get; set; } = AdvertisementDeliveryMode.Hud;
    public HudPosition Position { get; set; } = HudPosition.BottomLeft;
    public double DurationSeconds { get; set; } = 8;
}

internal sealed class AdvertisementHudConfig
{
    public AdvertisementDeliveryMode Mode { get; set; } = AdvertisementDeliveryMode.Chat;
    public HudPosition Position { get; set; } = HudPosition.BottomLeft;
    public double DurationSeconds { get; set; } = 8;
    public Dictionary<string, AdvertisementHudRule> Messages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class AdvertisementHudDelivery(IOptions<AdvertisementHudConfig> config, ILogger logger) : IDisposable
{
    internal const string Channel = "Advertisement.Messages";
    private ICustomHudApi? _hud;
    private bool _warned;
    private ICustomBannerApi? _banners;

    internal void Initialize(ICustomHudApi? hud, ICustomBannerApi? banners = null)
    {
        if (ReferenceEquals(_hud, hud) && ReferenceEquals(_banners, banners)) return;
        _hud?.ClearChannel(Channel);
        _hud = hud;
        _banners = banners;
    }

    // true запрещает вывод в чат. Явный HUD-only не меняет канал при ошибке; legacy сохраняет прежнее резервирование.
    internal bool Send(IPlayer player, string key, Func<string?> text, AdvertisementPresentation? presentation = null,
        IReadOnlyDictionary<string, object?>? parameters = null, string? language = null)
    {
        var settings = config.Value;
        settings.Messages.TryGetValue(key, out var rule);
        var mode = presentation is null ? rule?.Mode ?? settings.Mode : presentation.DisplayType switch
        {
            "chat" => AdvertisementDeliveryMode.Chat,
            "hud" => AdvertisementDeliveryMode.Hud,
            "chat_and_hud" => AdvertisementDeliveryMode.ChatAndHud,
            _ => (AdvertisementDeliveryMode)(-1)
        };
        var hudOnly = presentation?.DisplayType == "hud";
        if (mode == AdvertisementDeliveryMode.Chat || _hud is not { IsAvailable: true }) return hudOnly;
        var duration = presentation?.HudDurationSeconds ?? rule?.DurationSeconds ?? settings.DurationSeconds;
        var position = presentation is null ? rule?.Position ?? settings.Position : presentation.HudPosition switch
        {
            "top_left" => HudPosition.TopLeft, "top_center" => HudPosition.TopCenter, "top_right" => HudPosition.TopRight,
            "middle_left" => HudPosition.MiddleLeft, "center" => HudPosition.Center, "middle_right" => HudPosition.MiddleRight,
            "bottom_left" => HudPosition.BottomLeft, "bottom_center" => HudPosition.BottomCenter, "bottom_right" => HudPosition.BottomRight,
            _ => (HudPosition)(-1)
        };
        var style = presentation?.HudStyle switch
        {
            null or "notice" => HudMessageStyle.Notice, "banner" => HudMessageStyle.Banner,
            _ => (HudMessageStyle)(-1)
        };
        if ((presentation is { Template: null } && string.IsNullOrWhiteSpace(presentation.HudLocalizationKey)) || !Enum.IsDefined(mode) || !Enum.IsDefined(position) || !Enum.IsDefined(style) || !double.IsFinite(duration) || duration is < 0.5 or > 60)
        {
            if (!_warned) logger.LogWarning("[Advertisement] Некорректные настройки доставки HUD; доставка HUD пропущена");
            _warned = true;
            return hudOnly;
        }
        try
        {
            var options = new HudMessageOptions
            {
                Channel = Channel, Position = position, DurationSeconds = duration, Priority = 0, Style = style
            };
            if (presentation?.Template is { } template)
            {
                if (_banners is { IsAvailable: true })
                    _banners.ShowLocalized(player, template, new HudBannerContent
                    {
                        Header = presentation.HeaderKey, Title = presentation.TitleKey, Description = presentation.HudLocalizationKey
                    }, parameters ?? new Dictionary<string, object?>(), options, language);
                return hudOnly;
            }
            var content = text();
            if (string.IsNullOrWhiteSpace(content)) return hudOnly;
            var accepted = _hud.Show(player, content, options);
            return hudOnly || (accepted && mode == AdvertisementDeliveryMode.Hud);
        }
        catch (ArgumentException error)
        {
            if (!_warned) logger.LogWarning(error, "[Advertisement] Текст не принят Custom HUD; доставка HUD пропущена");
            _warned = true;
            return hudOnly;
        }
    }

    public void Dispose() => Initialize(null);
}
