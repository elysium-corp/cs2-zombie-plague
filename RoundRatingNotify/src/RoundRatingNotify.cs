using Common.Di;
using RoundRatingNotify.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Core.Utils.Extensions;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;

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
    private readonly Dictionary<string, int> _playersDamage = new();
    private readonly Dictionary<string, int> _playersInfect = new();

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
        
        _zombiePlagueApi.Events.Post.PlayerInfectEvent += OnPlayerInfected;
    }

    protected override void OnUnload()
    {
        core.GameEvent.Unhook(_guidOnEventRoundEndPost);
        core.GameEvent.Unhook(_guidOnEvEventPlayerHurtPost);
        
        _zombiePlagueApi.Events.Post.PlayerInfectEvent -= OnPlayerInfected;
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
        SendNotifyInChat();

        Clear();

        return HookResult.Continue;
    }

    private void OnPlayerInfected(ref PlayerInfectPostContext context)
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

    private void SendNotifyInChat()
    {
        var humanTopPlayer = GetTopPlayerAndResultInRound(Team.CT);
        var zombieTopPlayer = GetTopPlayerAndResultInRound(Team.T);
        
        if (humanTopPlayer.Key.IsNotNullOrEmpty())
        {
            core.PlayerManager.SendChat(
                $"{core.Localizer["RoundRatingNotify.prefix"]} [blue]Лучший игрок за людей: {humanTopPlayer.Key} — нанес {humanTopPlayer.Value} [blue]урона.");
        }
        
        if (zombieTopPlayer.Key.IsNotNullOrEmpty())
        {
            core.PlayerManager.SendChat(
                $"{core.Localizer["RoundRatingNotify.prefix"]} [red]Лучший игрок за зомби: {zombieTopPlayer.Key} — заразил {zombieTopPlayer.Value} [red]игроков.");
        }
    }

    private void Clear()
    {
        _playersDamage.Clear();
        _playersInfect.Clear();
    }
}