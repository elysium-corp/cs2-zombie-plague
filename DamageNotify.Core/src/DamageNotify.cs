using CustomHud.Api;
using Common.Di;
using Common.Di.Utils;
using DamageNotify.Core.Data.Configs;
using DamageNotify.Core.Di;
using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Api;

namespace DamageNotify.Core;

[PluginMetadata(
    Id = "DamageNotify.Core",
    Version = "0.2.0",
    Name = "[ZP] DamageNotify",
    Author = "illusion & fdrinv",
    Description = "Provides customizable notifications for in-game damage events")
]
internal partial class DamageNotify(ISwiftlyCore core) : Plugin<DamageNotifyModule>(core)
{
    private readonly Lazy<BannerNotificationClient> _notifications = GetRequiredServiceLazy<BannerNotificationClient>();

    private Guid _guidOnPlayerHurtPost = Guid.Empty;

    private IZombiePlagueApi _zombiePlagueApi = null!;

    private readonly Lazy<IOptions<DamageNotifyConfig>> _config = GetRequiredServiceLazy<IOptions<DamageNotifyConfig>>();

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        interfaceManager.TryGetSharedInterface<IBannerNotificationApi>(IBannerNotificationApi.SharedApiKey, out var notificationApi);
        _notifications.Value.Bind(notificationApi);
    }

    protected override void OnReady()
    {
        _guidOnPlayerHurtPost = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    protected override void OnUnload()
    {
        if (_notifications.IsValueCreated) _notifications.Value.Bind(null);
        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (player == null || victim == null || !player.IsValid || !victim.IsValid) return HookResult.Continue;

        if (player.IsFakeClient) return HookResult.Continue;

        if (_zombiePlagueApi.IsInfected(player) || victim.Controller.Team == player.Controller.Team)
        {
            return HookResult.Continue;
        }

        _notifications.Value.Publish(player, "Game.Damage.Hit", new Dictionary<string, object?>
        {
            ["victim"] = victim.Name, ["victim_health"] = victim.PlayerPawn?.Health ?? 0,
            ["damage"] = @event.ActualDmgHealth, ["weapon"] = @event.Weapon, ["hitgroup"] = @event.ActualHitGroup
        });

        return HookResult.Continue;
    }
}
