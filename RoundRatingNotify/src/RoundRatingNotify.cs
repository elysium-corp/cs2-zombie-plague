using CustomHud.Api;
using Common.Di;
using System.Globalization;
using Localization.Api;
using RoundRatingNotify.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;

namespace RoundRatingNotify;

[PluginMetadata(
    Id = "RoundRatingNotify.Core",
    Version = "0.2.0",
    Name = "[ZP] RoundRatingNotify",
    Author = "illusion & fdrinv",
    Description = "Print the best player of the round"
)]
internal sealed partial class RoundRatingNotify(ISwiftlyCore core) : Plugin<RoundRatingNotifyModule>(core)
{
    private readonly Lazy<BannerNotificationClient> _notifications = GetRequiredServiceLazy<BannerNotificationClient>();

    private readonly Dictionary<string, int> _playersDamage = new();
    private readonly Dictionary<string, int> _playersInfect = new();

    private Guid _guidOnEventRoundEndPost = Guid.Empty;
    private Guid _guidOnEvEventPlayerHurtPost = Guid.Empty;

    private IZombiePlagueApi _zombiePlagueApi = null!;
    private ILocalizationApi _localization = null!;

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
        _localization = interfaceManager.GetSharedInterface<ILocalizationApi>(ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        interfaceManager.TryGetSharedInterface<IBannerNotificationApi>(IBannerNotificationApi.SharedApiKey, out var notificationApi);
        _notifications.Value.Bind(notificationApi);
    }

    protected override void OnReady()
    {
        _guidOnEventRoundEndPost = core.GameEvent.HookPost<EventRoundEnd>(OnEventRoundEnd);
        _guidOnEvEventPlayerHurtPost = core.GameEvent.HookPost<EventPlayerHurt>(OnEventPlayerHurt);
        
        _zombiePlagueApi.Events.Players.Infected.Hook(OnPlayerInfected);
    }

    protected override void OnUnload()
    {
        if (_notifications.IsValueCreated) _notifications.Value.Bind(null);
        core.GameEvent.Unhook(_guidOnEventRoundEndPost);
        core.GameEvent.Unhook(_guidOnEvEventPlayerHurtPost);
        
        _zombiePlagueApi.Events.Players.Infected.Unhook(OnPlayerInfected);
    }

    private HookResult OnEventPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;

        if (player == null || !player.IsValid || _zombiePlagueApi.IsInfected(player))
        {
            return HookResult.Continue;
        }

        if (!_playersDamage.TryAdd(player.Name, @event.ActualDmgHealth))
        {
            _playersDamage[player.Name] += @event.ActualDmgHealth;
        }

        return HookResult.Continue;
    }

    private HookResult OnEventRoundEnd(EventRoundEnd @event)
    {
        SendNotifications();

        Clear();

        return HookResult.Continue;
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        var infector = context.Infector;

        if (infector is not { IsValid: true })
        {
            return;
        }

        if (!_playersInfect.TryAdd(infector.Name, 1))
        {
            _playersInfect[infector.Name]++;
        }
    }

    private KeyValuePair<string, int> GetTopPlayerAndResultInRound(Team team)
    {
        var searchDict = team == Team.CT ? _playersDamage : _playersInfect;
        var keyValuesPair = searchDict.OrderByDescending(pair => pair.Value).FirstOrDefault();
        return keyValuesPair;
    }

    private void SendNotifications()
    {
        var humanTopPlayer = GetTopPlayerAndResultInRound(Team.CT);
        var zombieTopPlayer = GetTopPlayerAndResultInRound(Team.T);
        
        foreach (var player in core.PlayerManager.GetAllPlayers()
                     .Where(value => value is { IsAuthorized: true, IsFakeClient: false }))
        {
            if (!string.IsNullOrEmpty(humanTopPlayer.Key))
                _notifications.Value.Publish(player, "RoundRatingNotify.HumanTop", new Dictionary<string, object?>
                    { ["player"] = humanTopPlayer.Key, ["value"] = humanTopPlayer.Value });
            if (!string.IsNullOrEmpty(zombieTopPlayer.Key))
                _notifications.Value.Publish(player, "RoundRatingNotify.ZombieTop", new Dictionary<string, object?>
                    { ["player"] = zombieTopPlayer.Key, ["value"] = zombieTopPlayer.Value });
        }
    }

    private void Clear()
    {
        _playersDamage.Clear();
        _playersInfect.Clear();
    }
}
