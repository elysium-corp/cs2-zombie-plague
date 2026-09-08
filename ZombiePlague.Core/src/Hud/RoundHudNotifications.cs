using CustomHud.Api;
using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Events.Contexts.Round;
using ZombiePlague.Core.Api.Events;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Hud.AbilityHud;
using PlayerManager = ZombiePlague.Core.Data.Managers.Contracts.IPlayerManager;

namespace ZombiePlague.Core.Hud;

internal sealed class RoundHudNotifications(ISwiftlyCore core, ZombiePlagueRoundEvents events,
    Func<ILocalizationApi> localization, IRoundManager rounds, PlayerManager players, IPlayerRepository preferences,
    AbilityHudService abilityHud, AbilityHudSettings abilitySettings) : IDisposable
{
    private IBannerNotificationApi? _notifications;
    private IDisposable? _context;
    private IDisposable? _configuration;
    private bool _started;

    internal void Initialize(IBannerNotificationApi? notifications)
    {
        if (ReferenceEquals(_notifications, notifications)) return;
        _context?.Dispose(); _configuration?.Dispose();
        _notifications = notifications;
        _context = notifications?.RegisterContext("ZombiePlague", Context);
        _configuration = notifications?.SubscribeConfiguration(ApplyWidget);
        ApplyWidget();
    }

    private void ApplyWidget()
    {
        if (_notifications?.GetWidget("ZombiePlague.Abilities") is not { } options) return;
        abilitySettings.ApplyServerOptions(options);
        abilityHud.ApplyServerOptions(options);
    }

    internal void Start()
    {
        if (_started) return;
        events.Started.Hook(OnRoundStarted); events.Ended.Hook(OnRoundEnded); _started = true;
    }

    private string Name(IPlayer player, RoundBase? round) => round is null ? "" :
        localization().GetForPlayer(player, $"ZombiePlague.Round.{LocalizationKey.Canonicalize(round.Id)}.Name") ?? round.Name;

    private IReadOnlyDictionary<string, object?> Context(IPlayer player) => new Dictionary<string, object?>
    {
        ["round_id"] = rounds.CurrentRound?.Id ?? "", ["round_name"] = Name(player, rounds.CurrentRound),
        ["next_round_id"] = rounds.NextRound?.Id ?? "", ["next_round_name"] = Name(player, rounds.NextRound),
        ["is_preparing"] = rounds.IsPreparing, ["is_zombie"] = players.IsZombie(player),
        ["humans"] = players.GetAllAliveHumans().Count(), ["zombies"] = players.GetAllAliveZombies().Count(),
        ["human_class"] = preferences.GetHClassId(player), ["zombie_class"] = preferences.GetZClassId(player)
    };

    private void OnRoundStarted(ref RoundStartedContext context)
    {
        if (_notifications is null) return;
        _notifications.Clear("ZombiePlague.Round.Preparing");
        foreach (var player in core.PlayerManager.GetAllPlayers().Where(player => player is { IsValid: true, IsAuthorized: true, IsFakeClient: false }))
        {
            var name = localization().GetForPlayer(player, $"ZombiePlague.Round.{LocalizationKey.Canonicalize(context.Round.Id)}.Name") ?? context.Round.Name;
            _notifications.Publish(player, "Game.Round.Started", new Dictionary<string, object?>
                { ["round_id"] = context.Round.Id, ["round_name"] = name });
        }
    }
    private void OnRoundEnded(ref RoundEndedContext context)
    {
        foreach (var key in new[] { "Game.Round.Started", "ZombiePlague.Round.Infection.FirstInfected", "ZombiePlague.Round.Nemesis.Selected", "ZombiePlague.Round.Survivor.Selected", "ZombiePlague.Round.Preparing" })
            _notifications?.Clear(key);
    }
    public void Dispose()
    {
        if (_started) { events.Started.Unhook(OnRoundStarted); events.Ended.Unhook(OnRoundEnded); _started = false; }
        Initialize(null);
    }
}
