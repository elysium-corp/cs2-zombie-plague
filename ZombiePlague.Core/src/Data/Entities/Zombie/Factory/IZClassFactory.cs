namespace ZombiePlague.Core.Data.Entities.Zombie.Factory;

internal interface IZClassFactory
{
    IZClass Create<TClass>(ulong steamId = 0) where TClass : IZClass;

    public IZClass CreateOrDefault(string classId, ulong steamId = 0);
}
