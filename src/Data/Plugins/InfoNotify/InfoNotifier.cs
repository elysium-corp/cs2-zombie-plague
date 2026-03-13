using CS2ZombiePlague.Config.InfoNotify;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data.Plugins.InfoNotify;

public sealed class InfoNotifier(ISwiftlyCore core, IOptions<InfoNotifierConfig> config) : IInfoNotifier
{
    private readonly InfoNotifierConfig _config = config.Value;
    
    private Guid _onPlayerConnectFull;
    private Guid _onRoundStart;
    private Guid _onRoundEnd;

    private CancellationTokenSource? _eventMessagesHandler;

    public void Initialize()
    {
        _onPlayerConnectFull = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        _onRoundStart = core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _onRoundEnd = core.GameEvent.HookPre<EventRoundEnd>(OnRoundEnd);
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }
        
        var playerConnectMessages = _config.PlayerConnectMessages;

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
        TryStartEventMessagesTimer();
        
        var roundStartMessages = _config.RoundStartMessages;

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
        var roundEndMessages = _config.RoundEndMessages;

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
        _eventMessagesHandler?.Cancel();
        
        var roundEventMessages = _config.RoundEventMessages;

        if (roundEventMessages.Count == 0)
        {
            return;
        }

        var delayBeforeFirstMessages = _config.TimeBetweenEventMessagesPerSeconds;
        var timeBetweenMessages =  _config.TimeBetweenEventMessagesPerSeconds;
        var randomEventMessagesEnable = _config.RandomEventMessagesEnable;
        
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
    
    private void SendChatEventMessages(List<string> messages)
    {
        foreach (var message in messages)
        {
            core.PlayerManager.SendChat(message);
        }
    }
    
    private void SendChatRandomEventMessages(List<string> messages)
    {
        var randomMessages = messages.Shuffle().ToList();
        var countRandomEventMessages = _config.CountRandomEventMessages <= randomMessages.Count ? _config.CountRandomEventMessages :  randomMessages.Count;
        
        for (short index = 0; index < countRandomEventMessages; index++)
        {
            core.PlayerManager.SendChat(randomMessages[index]);
        }
    }
}