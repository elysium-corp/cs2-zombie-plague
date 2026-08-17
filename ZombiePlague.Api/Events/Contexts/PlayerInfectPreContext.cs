using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts;

public struct PlayerInfectPreContext(IPlayer player, IPlayer? infector = null) : IPreHookContext
{
    public IPlayer Player { get; set; } = player;

    public IPlayer? Infector { get; set; } = infector;

    public bool IsCancelled { get; private set; }

    public void Cancel()
    {
        IsCancelled = true;
    }
}