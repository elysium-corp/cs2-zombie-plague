using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts.Player;

/// <summary>
/// Контекст завершённого заражения игрока.
/// </summary>
public readonly struct PlayerInfectedContext(IPlayer player, IPlayer? infector = null) : IPostHookContext
{
    /// <summary>Заражённый игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Игрок-источник заражения, если он известен.</summary>
    public IPlayer? Infector { get; } = infector;
}
