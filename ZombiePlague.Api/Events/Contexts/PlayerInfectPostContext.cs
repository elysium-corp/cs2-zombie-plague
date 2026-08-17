using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts;

public struct PlayerInfectPostContext(IPlayer player, IPlayer? infector = null) : IPostHookContext
{
    public IPlayer Player { get; set; } = player;

    public IPlayer? Infector { get; set; } = infector;
}