using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data;

public class RoundRatingNotify(ISwiftlyCore core, IEventSubscriber eventSubscriber)
{
    private readonly Dictionary<IPlayer, int> _playersDamage = new();
    private readonly Dictionary<IPlayer, int> _playersInfect = new();

    public void Start()
    {
        core.GameEvent.HookPost<EventRoundEnd>(OnEventRoundEnd);
        core.GameEvent.HookPost<EventPlayerHurt>(OnEventPlayerHurt);
        eventSubscriber.OnPlayerInfectedBy += OnPlayerInfectedBy;
    }

    private HookResult OnEventPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        if (player == null || !player.IsValid || player.IsInfected())
        {
            return HookResult.Continue;
        }

        if (!_playersDamage.TryAdd(player, @event.DmgHealth))
        {
            _playersDamage[player] += @event.DmgHealth;
        }

        return HookResult.Continue;
    }

    private HookResult OnEventRoundEnd(EventRoundEnd @event)
    {
        SendNotifyInChat();
        ClearDictionaries();
        return HookResult.Continue;
    }

    private void OnPlayerInfectedBy(IPlayer infector, IPlayer player)
    {
        if (!_playersInfect.TryAdd(infector, 1))
        {
            _playersInfect[infector]++;
        }
    }

    private KeyValuePair<IPlayer?, int> GetTopPlayerAndResultInRound(Team team)
    {
        var searchDict = team == Team.CT ? _playersDamage : _playersInfect;
        var keyValuesPair = searchDict.OrderByDescending(pair => pair.Value).FirstOrDefault();
        return keyValuesPair;
    }

    private void SendNotifyInChat()
    {
        var humanKeyValuesPair = GetTopPlayerAndResultInRound(Team.CT);
        var zombieKeyValuePair = GetTopPlayerAndResultInRound(Team.T);

        var humanPlayer = humanKeyValuesPair.Key;
        if (humanPlayer == null)
        {
            return;
        }

        var humanName = humanPlayer.Name;
        var humanDamage = humanKeyValuesPair.Value;
        core.PlayerManager.SendChat($"[blue]Лучший игрок за людей: {humanName} — нанес {humanDamage} [blue]урона.");

        var zombiePlayer = zombieKeyValuePair.Key;
        if (zombiePlayer == null)
        {
            return;
        }

        var zombieName = zombiePlayer.Name;
        var zombieInfect = zombieKeyValuePair.Value;
        core.PlayerManager.SendChat($"[red]Лучший игрок за зомби: {zombieName} — заразил {zombieInfect} [red]игроков.");
    }

    private void ClearDictionaries()
    {
        _playersDamage.Clear();
        _playersInfect.Clear();
    }
}