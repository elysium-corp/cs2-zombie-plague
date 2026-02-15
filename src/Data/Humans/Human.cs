using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Humans;

public class Human : IHuman
{
    public IPlayer Player { get; set; }
    public bool IsSurvivor { get; set; } = false;

    public Human(IPlayer player, bool isSurvivor = false)
    {
        Player = player;
        IsSurvivor = isSurvivor;
    }
}