using CS2ZombiePlague.Data.Weapons.Contracts;

namespace CS2ZombiePlague.Data.Weapons;

public interface IGrenadeFactory
{
    public BaseGrenade Create<T>() where T : BaseGrenade;
    
    public BaseGrenade Create(string internalName);
}