using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;

namespace ZombiePlague.Core.Data.Entities.Zombie.Factory;

internal sealed class ZClassFactory(ZombieCatalogService catalog, AbilityFactory abilityFactory) : IZClassFactory
{
    public IZClass Create<TClass>(ulong steamId = 0) where TClass : IZClass
    {
        var snapshot = catalog.Current;
        if (typeof(TClass) == typeof(ZNemesis)) return Create(snapshot, snapshot.Document.NemesisClass, steamId);
        var key = typeof(TClass) switch
        {
            var type when type == typeof(ZCleric) => "zombie_cleric",
            var type when type == typeof(ZHunter) => "zombie_hunter",
            var type when type == typeof(ZAssassin) => "zombie_assassin",
            var type when type == typeof(ZHeavy) => "zombie_heavy",
            var type when type == typeof(ZSmoker) => "zombie_smoker",
            _ => throw new NotSupportedException("Неизвестный тип класса")
        };
        return CreateOrDefault(key, steamId);
    }

    public IZClass CreateOrDefault(string classId, ulong steamId = 0)
    {
        var snapshot = catalog.Current;
        var definition = snapshot.Document.Classes.FirstOrDefault(item => item.InternalName == classId && item.Enabled && item.Kind == "zombie")
            ?? snapshot.Document.Classes.Single(item => item.InternalName == snapshot.Document.DefaultClass);
        return Create(snapshot, definition.InternalName, steamId);
    }

    private IZClass Create(ZombieCatalogState snapshot, string key, ulong steamId)
    {
        var definition = snapshot.Document.Classes.Single(item => item.InternalName == key);
        // Класс и его способности берутся из одного снимка, даже если в этот момент завершился reload
        var abilities = snapshot.ResolveAbilities(definition.Abilities, steamId, AbilitySide.Zombie).Select(abilityFactory.Create).ToList();
        return definition.Kind == "nemesis" ? new ZNemesis(definition, abilities) : new ZCatalogClass(definition, abilities);
    }
}
