namespace ZombiePlague.Core.Data.Entities.Zombie;

internal interface IZombie : IPlayerRole
{
    public IZClass ZClass { get; }
}