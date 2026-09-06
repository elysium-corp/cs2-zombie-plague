using System.Text.Json;
using Localization.Api;
using ZombiePlague.Core.Config.Human;

namespace ZombiePlague.Core.Catalog;

internal static class CatalogUpgrade
{
    public static void Apply(ZombieCatalogDocument document, HClassConfig humans)
    {
        if (document.FormatVersion != 1) return;
        AddHuman(document, humans.Mercenary, "human");
        AddHuman(document, humans.Survivor, "survivor");
        document.DefaultHumanClass = humans.Mercenary.InternalName;
        document.SurvivorClass = humans.Survivor.InternalName;
        foreach (var item in document.Classes)
        {
            var prefix = $"ZombiePlague.{(item.Kind is "human" or "survivor" ? "HClass" : "ZClass")}.{LocalizationKey.Canonicalize(item.InternalName)}";
            if (item.DisplayNameKey == "") item.DisplayNameKey = prefix + ".Name";
            if (item.DescriptionKey == "") item.DescriptionKey = prefix + ".Description";
        }
        foreach (var item in document.Abilities)
        {
            var prefix = $"ZombiePlague.Ability.{LocalizationKey.Canonicalize(item.InternalName)}";
            if (item.DisplayNameKey == "") item.DisplayNameKey = prefix + ".Name";
            if (item.DescriptionKey == "") item.DescriptionKey = prefix + ".Description";
        }
        document.FormatVersion = 2;
    }

    private static void AddHuman(ZombieCatalogDocument document, IHClassConfig source, string kind)
    {
        // Конфликт ID не должен незаметно заменять уже настроенный класс
        ZombieCatalogDocument.Require(document.Classes.All(item => item.InternalName != source.InternalName),
            $"Ключ человеческого класса {source.InternalName} уже занят в каталоге");
        var item = JsonSerializer.Deserialize<ZombieClassDefinition>(JsonSerializer.Serialize(source), ZombieCatalogDocument.JsonOptions)!;
        item.Kind = kind;
        item.SortOrder = document.Classes.Count;
        // В старом формате фабрика пропускала удалённые способности; сохраняем это поведение при переносе
        item.Abilities = item.Abilities.Where(key => document.Abilities.Any(ability => ability.InternalName == key)).Distinct().ToList();
        document.Classes.Add(item);
    }
}
