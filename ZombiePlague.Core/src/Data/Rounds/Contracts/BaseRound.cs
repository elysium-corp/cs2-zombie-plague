using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class BaseRound : IRound
{
    public abstract int Chance { get; }
    public abstract string Name { get; }
    
    public abstract void Start();
    public abstract void End();

    protected virtual bool CanInfect(IPlayer attacker, IPlayer victim) { return true; }
}