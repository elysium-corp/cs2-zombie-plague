using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Humans;

internal class Human(IPlayer player, bool isSurvivor = false) : IHuman
{
    public IPlayer Player { get; set; } = player;
    
    public bool IsSurvivor { get; set; } = isSurvivor;
}