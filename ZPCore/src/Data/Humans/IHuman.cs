using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Humans;

internal interface IHuman
{
    public IPlayer Player { get; set; }
    public bool IsSurvivor { get; set; }
}