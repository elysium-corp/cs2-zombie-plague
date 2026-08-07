namespace ZombiePlague.Core.Data.Entities.Zombie.Factory;

internal interface IZClassFactory
{
    IZClass Create<TClass>() where TClass : IZClass;

    public IZClass CreateOrDefault(string classId);
}