using ZPCore.Data.Abilities.Contracts;

namespace ZPCore.Data.Zombies.ZClasses;

internal interface IZClassFactory
{
    public IAbility Create<T>() where T : IAbility;
}