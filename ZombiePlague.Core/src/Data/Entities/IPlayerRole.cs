using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Entities;

internal interface IPlayerRole
{
    IPlayer Owner { get; }
    
    void Bind();

    void Unbind();
}