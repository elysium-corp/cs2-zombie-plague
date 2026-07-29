namespace ZombiePlague.Core.Data.Entities.Human.Factory;

internal interface IHClassFactory
{
    IHClass Create<TClass>() where TClass : IHClass;

    IHClass CreateOrDefault(string classId);
}