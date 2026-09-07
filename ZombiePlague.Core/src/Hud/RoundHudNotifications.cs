using CustomHud.Api;
using Localization.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Api.Events.Contexts.Round;
using ZombiePlague.Core.Api.Events;

namespace ZombiePlague.Core.Hud;

internal sealed class RoundHudConfig
{
    public bool Enabled { get; set; } = true;
    public double DurationSeconds { get; set; } = 6;
    public HudPosition Position { get; set; } = HudPosition.TopCenter;
}

internal sealed class RoundHudNotifications(ISwiftlyCore core, ZombiePlagueRoundEvents events,
    Func<ILocalizationApi> localization, IOptions<RoundHudConfig> config) : IDisposable
{
    internal const string Channel = "ZombiePlague.Round";
    private ICustomHudApi? _hud;
    private bool _started;
    private HudMessageOptions? _options;

    internal void Initialize(ICustomHudApi? hud)
    {
        if (ReferenceEquals(_hud, hud)) return;
        _hud?.ClearChannel(Channel);
        _hud = hud;
    }

    internal void Start()
    {
        if (_started || !config.Value.Enabled) return;
        var settings = config.Value;
        if (!double.IsFinite(settings.DurationSeconds) || settings.DurationSeconds is < 0.5 or > 60 || !Enum.IsDefined(settings.Position))
        {
            core.Logger.LogWarning("[RoundHud] Некорректные DurationSeconds или Position в round_hud.json; баннеры отключены");
            return;
        }
        _options = new HudMessageOptions
        {
            Channel = Channel, Position = settings.Position, DurationSeconds = settings.DurationSeconds,
            Priority = 100, Style = HudMessageStyle.Banner
        };
        events.Started.Hook(OnRoundStarted);
        events.Ended.Hook(OnRoundEnded);
        _started = true;
    }

    private void OnRoundStarted(ref RoundStartedContext context)
    {
        if (_hud is not { IsAvailable: true } || _options is null) return;
        var key = context.Round.Id switch
        {
            RoundIds.Infection => "ZombiePlague.Round.Infection.Name",
            RoundIds.Plague => "ZombiePlague.Round.Plague.Started",
            RoundIds.Nemesis => "ZombiePlague.Round.Nemesis.Name",
            RoundIds.Survivor => "ZombiePlague.Round.Survivor.Name",
            _ => null
        };
        var api = localization();
        foreach (var player in core.PlayerManager.GetAllPlayers()
                     .Where(player => player is { IsValid: true, IsAuthorized: true, IsFakeClient: false }))
        {
            var name = key is null ? context.Round.Name : api.GetForPlayer(player, key) ?? context.Round.Name;
            var text = api.GetForPlayer(player, "ZombiePlague.Round.Hud.Started",
                new Dictionary<string, string> { ["mode"] = HudText.Escape(name) }) ?? HudText.Escape(name);
            _hud.Show(player, text, _options);
        }
    }

    private void OnRoundEnded(ref RoundEndedContext context) => _hud?.ClearChannel(Channel);

    public void Dispose()
    {
        if (_started)
        {
            events.Started.Unhook(OnRoundStarted);
            events.Ended.Unhook(OnRoundEnded);
            _started = false;
        }
        Initialize(null);
    }
}
