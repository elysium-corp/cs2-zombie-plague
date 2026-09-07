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

    internal void Initialize(ICustomHudApi? hud)
    {
        if (ReferenceEquals(_hud, hud)) return;
        _hud?.ClearChannel(Channel);
        _hud = hud;
    }

    // true означает, что HUD принят как единственный способ вывода; иначе вызывающий код отправляет обычный чат
    internal bool Send(IPlayer player, string key, Func<string> text)
    {
        var settings = config.Value;
        settings.Messages.TryGetValue(key, out var rule);
        var mode = rule?.Mode ?? settings.Mode;
        if (mode == AdvertisementDeliveryMode.Chat || _hud is not { IsAvailable: true }) return false;
        var duration = rule?.DurationSeconds ?? settings.DurationSeconds;
        var position = rule?.Position ?? settings.Position;
        if (!Enum.IsDefined(mode) || !Enum.IsDefined(position) || !double.IsFinite(duration) || duration is < 0.5 or > 60)
        {
            if (!_warned) logger.LogWarning("[Advertisement] Некорректные настройки hud_delivery.json; используется чат");
            _warned = true;
            return false;
        }
        try
        {
            var accepted = _hud.Show(player, text(), new HudMessageOptions
            {
                Channel = Channel, Position = position, DurationSeconds = duration, Priority = 0
            });
            return accepted && mode == AdvertisementDeliveryMode.Hud;
        }
        catch (ArgumentException error)
        {
            if (!_warned) logger.LogWarning(error, "[Advertisement] Текст не принят Custom HUD; используется чат");
            _warned = true;
            return false;
        }
    }

    public void Dispose() => Initialize(null);
}
