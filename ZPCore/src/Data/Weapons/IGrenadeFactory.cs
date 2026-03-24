using ZPCore.Data.Weapons.Contracts;

namespace ZPCore.Data.Weapons;

internal interface IGrenadeFactory
{
    public BaseGrenade Create<T>() where T : BaseGrenade;
    
    public BaseGrenade Create(string internalName);
}