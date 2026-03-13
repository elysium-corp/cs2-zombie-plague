using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Humans;

public class Human(IPlayer player, bool isSurvivor = false) : IHuman
{
    public IPlayer Player { get; set; } = player;
    public bool IsSurvivor { get; set; } = isSurvivor;
}