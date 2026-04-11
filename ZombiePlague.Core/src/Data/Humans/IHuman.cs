using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Humans;

internal interface IHuman
{
    public IPlayer Player { get; set; }
    
    public bool IsSurvivor { get; set; }
}