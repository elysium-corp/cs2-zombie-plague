using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Entities.Human.Classes;

namespace ZombiePlague.Core.Data.Entities.Human.Factory;

internal sealed class HClassFactory(ZombieCatalogService catalog, AbilityFactory abilityFactory) : IHClassFactory
{
    public IHClass Create<TClass>(ulong steamId = 0) where TClass : IHClass
    {
        var snapshot = catalog.Current;
        return typeof(TClass) switch
        {
            var type when type == typeof(HMercenary) => Create(snapshot, snapshot.Document.DefaultHumanClass, steamId),
            var type when type == typeof(HSurvivor) => Create(snapshot, snapshot.Document.SurvivorClass, steamId),
            _ => throw new NotSupportedException("Неизвестный тип человеческого класса")
        };
    }

    public IHClass CreateOrDefault(string classId, ulong steamId = 0)
    {
        var snapshot = catalog.Current;
        var definition = snapshot.Document.Classes.FirstOrDefault(item => item.InternalName == classId && item.Enabled && item.Kind == "human")
            ?? snapshot.Document.Classes.Single(item => item.InternalName == snapshot.Document.DefaultHumanClass);
        return Create(snapshot, definition.InternalName, steamId);
    }

    private IHClass Create(ZombieCatalogState snapshot, string key, ulong steamId)
    {
        var definition = snapshot.Document.Classes.Single(item => item.InternalName == key);
        var abilities = snapshot.ResolveAbilities(definition.Abilities, steamId, AbilitySide.Human).Select(abilityFactory.Create).ToList();
        return definition.Kind == "survivor" ? new HSurvivor(definition, abilities) : new HMercenary(definition, abilities);
    }
}
