using Common.Di;
using RoundRatingNotify.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace RoundRatingNotify;

[PluginMetadata(
    Id = "RoundRatingNotify.Core",
    Version = "0.1.0",
    Name = "[ZP] RoundRatingNotify",
    Author = "illusion & fdrinv",
    Description = "Print the best player of the round"
)]
internal sealed partial class RoundRatingNotify(ISwiftlyCore core) : Plugin<RoundRatingNotifyModule>(core)
{
    private readonly Dictionary<IPlayer, int> _playersDamage = new();
    private readonly Dictionary<IPlayer, int> _playersInfect = new();

    private Guid _guidOnEventRoundEndPost = Guid.Empty;
    private Guid _guidOnEvEventPlayerHurtPost = Guid.Empty;
    
    private IZombiePlagueApi _zombiePlagueApi = null!;
    
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }
    
    protected override void OnReady()
    {
        _guidOnEventRoundEndPost = core.GameEvent.HookPost<EventRoundEnd>(OnEventRoundEnd);
        _guidOnEvEventPlayerHurtPost = core.GameEvent.HookPost<EventPlayerHurt>(OnEventPlayerHurt);
        _zombiePlagueApi.EventSubscriber.OnPlayerInfected += OnPlayerInfected;
    }

    protected override void OnUnload()
    {
        core.GameEvent.Unhook(_guidOnEventRoundEndPost);
        core.GameEvent.Unhook(_guidOnEvEventPlayerHurtPost);
        _zombiePlagueApi.EventSubscriber.OnPlayerInfected -= OnPlayerInfected;
    }

    private HookResult OnEventPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        
        if (player == null || !player.IsValid || _zombiePlagueApi.IsInfected(player))
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
        
        Clear();
        
        return HookResult.Continue;
    }

    private void OnPlayerInfected(IPlayer _, IPlayer? infector)
    {
        if (infector is not { IsValid: true })
        {
            return;
        }

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

    private void Clear()
    {
        _playersDamage.Clear();
        _playersInfect.Clear();
    }
}
