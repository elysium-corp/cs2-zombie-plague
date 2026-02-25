using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Rounds;

public abstract class BaseRound : IRound
{
    public abstract int Chance { get; }
    public abstract string Name { get; }
    
    public abstract void Start();
    public abstract void End();

    protected virtual bool CanInfect(IPlayer attacker, IPlayer victim) { return true; }
}