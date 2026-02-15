using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Humans;

public interface IHuman
{
    public IPlayer Player { get; set; }
    public bool IsSurvivor { get; set; }
}