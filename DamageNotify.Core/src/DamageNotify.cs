using Common.Di;
using Common.Di.Utils;
using DamageNotify.Core.Data.Configs;
using DamageNotify.Core.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Api;

namespace DamageNotify.Core;

[PluginMetadata(
    Id = "DamageNotify.Core", 
    Version = "0.1.0", 
    Name = "[ZP] DamageNotify", 
    Author = "illusion & fdrinv",
    Description = "Provides customizable notifications for in-game damage events")
]
internal partial class DamageNotify(ISwiftlyCore core) : Plugin<DamageNotifyModule>(core)
{
    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    
    private IZombiePlagueApi _zombiePlagueApi = null!;
    
    private readonly Lazy<IOptions<DamageNotifyConfig>> _config = GetRequiredServiceLazy<IOptions<DamageNotifyConfig>>();
    
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnReady()
    {
        _guidOnPlayerHurtPost = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    protected override void OnUnload()
    {
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

        var locale = core.Translation.GetPlayerLocalizer(player);
        var name = victim.Controller.PlayerName;
        var health = victim.RequiredPlayerPawn.Health;
        var dmgHealth = @event.DmgHealth;
        
        player.SendCenterHTML(
            duration: _config.Get().DurationMs,
            message:  $"<font color='#FFFFFF'>{locale["DamageNotify.HitMessage"]} </font>" +
                      $"<font color='#FF3333'>{name}</font><br>" +
                      $"<font color='#CCFF00'>{health}</font>" +
                      $" <font color='#FFFFFF'></font> " +
                      $"<font color='#FF3333'>-{dmgHealth}</font>"
        );

        return HookResult.Continue;
    }
}