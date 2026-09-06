namespace ZombiePlague.Core.Data.Entities.Human.Factory;

internal interface IHClassFactory
{
    IHClass Create<TClass>(ulong steamId = 0) where TClass : IHClass;

    IHClass CreateOrDefault(string classId, ulong steamId = 0);
}