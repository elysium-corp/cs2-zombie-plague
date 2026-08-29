using Common.Di;
using Common.Di.Utils;
using InfoNotify.Core.Data.Configs;
using InfoNotify.Core.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace InfoNotify.Core;

[PluginMetadata(
    Id = "InfoNotify.Core",
    Version = "0.1.0",
    Name = "[ZP] InfoNotify",
    Author = "illusion & fdrinv",
    Description = "Print useful information to the chat"
)]
internal sealed partial class InfoNotify(ISwiftlyCore core) : Plugin<InfoNotifyModule>(core)
{
    private readonly Lazy<IOptions<InfoNotifyConfig>> _config = GetRequiredServiceLazy<IOptions<InfoNotifyConfig>>();
    
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnRoundStartPost = Guid.Empty;
    private Guid _guidOnRoundEndPost = Guid.Empty;
    
    private CancellationTokenSource? _eventMessagesHandler;

    protected override void OnReady()
    {
        _guidOnPlayerConnectFullPost = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnRoundStartPost = core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _guidOnRoundEndPost = core.GameEvent.HookPre<EventRoundEnd>(OnRoundEnd);
    }

    protected override void OnUnload()
    {
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
        
        foreach (var message in playerConnectMessages)
        {
            player.SendChat(message);
        }
        
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
        
        foreach (var message in roundStartMessages)
        {
            core.PlayerManager.SendChat(message);
        }
        
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
        
        foreach (var message in roundEndMessages)
        {
            core.PlayerManager.SendChat(message);
        }
        
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
                    SendChatRandomEventMessages(roundEventMessages);
                }
                else
                {
                    SendChatEventMessages(roundEventMessages);
                }
            });
    }

    private void StopEventMessagesTimer()
    {
        _eventMessagesHandler?.Cancel();
        _eventMessagesHandler?.Dispose();
        _eventMessagesHandler = null;
    }
    
    private void SendChatEventMessages(List<string> messages)
    {
        foreach (var message in messages)
        {
            core.PlayerManager.SendChat(message);
        }
    }
    
    private void SendChatRandomEventMessages(List<string> messages)
    {
        var config = _config.Get();
        var randomMessages = messages.Shuffle().ToList();
        var countRandomEventMessages = config.CountRandomEventMessages <= randomMessages.Count ? config.CountRandomEventMessages :  randomMessages.Count;
        
        for (short index = 0; index < countRandomEventMessages; index++)
        {
            core.PlayerManager.SendChat(randomMessages[index]);
        }
    }
}
