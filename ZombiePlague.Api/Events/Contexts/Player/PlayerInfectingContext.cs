using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts.Player;

/// <summary>
/// Контекст попытки заражения до изменения роли игрока.
/// </summary>
public struct PlayerInfectingContext(IPlayer player, IPlayer? infector = null) : IPreHookContext
{
    /// <summary>Игрок, которого требуется заразить.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Игрок-источник заражения, если он известен.</summary>
    public IPlayer? Infector { get; set; } = infector;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
