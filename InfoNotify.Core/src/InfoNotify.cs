using CustomHud.Api;
using Common.Di;
using Common.Di.Utils;
using InfoNotify.Core.Data.Configs;
using InfoNotify.Core.Di;
using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace InfoNotify.Core;

[PluginMetadata(
    Id = "InfoNotify.Core",
    Version = "0.2.0",
    Name = "[ZP] InfoNotify",
    Author = "illusion & fdrinv",
    Description = "Print useful information to the chat"
)]
internal sealed partial class InfoNotify(ISwiftlyCore core) : Plugin<InfoNotifyModule>(core)
{
    private readonly Lazy<BannerNotificationClient> _notifications = GetRequiredServiceLazy<BannerNotificationClient>();

    private readonly Lazy<IOptions<InfoNotifyConfig>> _config = GetRequiredServiceLazy<IOptions<InfoNotifyConfig>>();
    private readonly Lazy<Func<ILocalizationApi>> _localization = GetRequiredServiceLazy<Func<ILocalizationApi>>();
    
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnRoundStartPost = Guid.Empty;
    private Guid _guidOnRoundEndPost = Guid.Empty;
    
    private CancellationTokenSource? _eventMessagesHandler;

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        interfaceManager.TryGetSharedInterface<IBannerNotificationApi>(IBannerNotificationApi.SharedApiKey, out var notificationApi);
        _notifications.Value.Bind(notificationApi);
    }

    protected override void OnReady()
    {
        _guidOnPlayerConnectFullPost = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnRoundStartPost = core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _guidOnRoundEndPost = core.GameEvent.HookPre<EventRoundEnd>(OnRoundEnd);
    }

    protected override void OnUnload()
    {
        if (_notifications.IsValueCreated) _notifications.Value.Bind(null);
        StopEventMessagesTimer();
        core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        core.GameEvent.Unhook(_guidOnRoundStartPost);
        core.GameEvent.Unhook(_guidOnRoundEndPost);
    }
    
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        if (!_config.Get().Enable) return HookResult.Continue;
        var player = @event.UserIdPlayer;
        
        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }
        
        var playerConnectMessages = _config.Get().PlayerConnectMessages;

        if (playerConnectMessages.Count == 0)
        {
            return HookResult.Continue;
        }
        
        SendKeysToPlayer(player, playerConnectMessages, "InfoNotify.Connected");
        
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        if (!_config.Get().Enable) return HookResult.Continue;
        TryStartEventMessagesTimer();
        
        var roundStartMessages = _config.Get().RoundStartMessages;

        if (roundStartMessages.Count == 0)
        {
            return HookResult.Continue;
        }
        
        SendKeysToAll(roundStartMessages, "InfoNotify.RoundStart");
        
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        StopEventMessagesTimer();
        if (!_config.Get().Enable) return HookResult.Continue;
        var roundEndMessages = _config.Get().RoundEndMessages;

        if (roundEndMessages.Count == 0)
        {
            return HookResult.Continue;
        }
        
        SendKeysToAll(roundEndMessages, "InfoNotify.RoundEnd");
        
        return HookResult.Continue;
    }
    
    private void TryStartEventMessagesTimer()
    {
        var config =  _config.Get();
        StopEventMessagesTimer();
        
        var roundEventMessages = config.RoundEventMessages;

        if (roundEventMessages.Count == 0)
        {
            return;
        }

        var delayBeforeFirstMessages = Math.Max(0.05f, config.DelayBeforeFirstEventMessagesPerSeconds);
        var timeBetweenMessages = Math.Max(1f, config.TimeBetweenEventMessagesPerSeconds);
        var randomEventMessagesEnable = config.RandomEventMessagesEnable;
        
        _eventMessagesHandler = core.Scheduler.DelayAndRepeatBySeconds(delayBeforeFirstMessages, timeBetweenMessages,
            () =>
            {
                if (randomEventMessagesEnable)
                {
                    SendRandomEventMessages(roundEventMessages);
                }
                else
                {
                    SendEventMessages(roundEventMessages);
                }
            });
    }

    private void StopEventMessagesTimer()
    {
        _eventMessagesHandler?.Cancel();
        _eventMessagesHandler = null;
    }
    
    private void SendEventMessages(List<string> messages)
    {
        SendKeysToAll(messages);
    }
    
    private void SendRandomEventMessages(List<string> messages)
    {
        var config = _config.Get();
        var randomMessages = messages.Shuffle().ToList();
        var countRandomEventMessages = config.CountRandomEventMessages <= randomMessages.Count ? config.CountRandomEventMessages :  randomMessages.Count;
        
        for (short index = 0; index < countRandomEventMessages; index++)
        {
            SendKeyToAll(randomMessages[index]);
        }
    }

    private void SendKeysToAll(IEnumerable<string> keys, string eventKey = "InfoNotify.Periodic")
    {
        foreach (var key in keys)
        {
            SendKeyToAll(key, eventKey);
        }
    }

    private void SendKeyToAll(string key, string eventKey = "InfoNotify.Periodic")
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        foreach (var player in core.PlayerManager.GetAllPlayers()
                     .Where(value => value is { IsAuthorized: true, IsFakeClient: false }))
        {
            var message = _localization.Value().GetForPlayer(player, key);
            if (!string.IsNullOrWhiteSpace(message))
            {
                // Старые информационные строки могли содержать управляющие цвета чата.
                message = System.Text.RegularExpressions.Regex.Replace(message,
                    @"\[(?:default|white|green|lightgreen|red|darkred|gold|yellow|blue|lightblue|grey|gray|orange|purple|olive|lime|bluegrey|darkblue|lightred|lightyellow)\]|[\x01-\x10]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                _notifications.Value.Publish(player, eventKey, new Dictionary<string, object?> { ["message"] = message });
            }
        }
    }

    private void SendKeysToPlayer(IPlayer player, IEnumerable<string> keys, string eventKey = "InfoNotify.Periodic")
    {
        foreach (var key in keys.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var message = _localization.Value().GetForPlayer(player, key);
            if (!string.IsNullOrWhiteSpace(message))
            {
                // Старые информационные строки могли содержать управляющие цвета чата.
                message = System.Text.RegularExpressions.Regex.Replace(message,
                    @"\[(?:default|white|green|lightgreen|red|darkred|gold|yellow|blue|lightblue|grey|gray|orange|purple|olive|lime|bluegrey|darkblue|lightred|lightyellow)\]|[\x01-\x10]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                _notifications.Value.Publish(player, eventKey, new Dictionary<string, object?> { ["message"] = message });
            }
        }
    }
}
