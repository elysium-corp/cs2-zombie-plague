using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Zombies.ZClasses;

internal interface IZClassFactory
{
    public IAbility Create<T>() where T : IAbility;
}