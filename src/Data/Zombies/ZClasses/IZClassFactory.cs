using CS2ZombiePlague.Data.Abilities.Contracts;

namespace CS2ZombiePlague.Data.Zombies.ZClasses;

public interface IZClassFactory
{
    public IAbility Create<T>() where T : IAbility;
}