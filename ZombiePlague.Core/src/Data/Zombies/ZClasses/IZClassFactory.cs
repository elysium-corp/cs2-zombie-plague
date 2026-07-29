namespace ZombiePlague.Core.Data.Zombies.ZClasses;

internal interface IZClassFactory
{
    public IZClass Create<TClass>() where TClass : IZClass;
}