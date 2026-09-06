using System.Text.Json;
using System.Text.Json.Nodes;
using Localization.Api;

namespace ZombiePlague.Core.Catalog;

internal static class LegacyZombieCatalog
{
    // Однократное преобразование существующих файлов сохраняет индивидуальные параметры сервера
    public static ZombieCatalogDocument? TryRead(string directory)
    {
        var classPath = Path.Combine(directory, "zombie_class.json");
        var abilityPath = Path.Combine(directory, "ability.json");
        if (!File.Exists(classPath) && !File.Exists(abilityPath)) return null;
        var classes = Read(classPath)["ZClassConfig"]?.AsObject()
            ?? throw new InvalidDataException("Отсутствует ZClassConfig");
        var abilities = Read(abilityPath)["AbilityConfig"]?.AsObject()
            ?? throw new InvalidDataException("Отсутствует AbilityConfig");
        var document = new ZombieCatalogDocument();
        foreach (var pair in classes)
        {
            var item = pair.Value.Deserialize<ZombieClassDefinition>(ZombieCatalogDocument.JsonOptions)
                ?? throw new InvalidDataException("Пустой старый класс");
            item.Kind = pair.Key == "Nemesis" ? "nemesis" : "zombie";
            item.SortOrder = document.Classes.Count;
            item.DisplayNameKey = $"ZombiePlague.ZClass.{LocalizationKey.Canonicalize(item.InternalName)}.Name";
            item.DescriptionKey = $"ZombiePlague.ZClass.{LocalizationKey.Canonicalize(item.InternalName)}.Description";
            document.Classes.Add(item);
        }
        foreach (var pair in abilities)
        {
            var parameters = pair.Value?.DeepClone().AsObject() ?? throw new InvalidDataException("Пустая старая способность");
            var enabled = parameters["Enable"]?.GetValue<bool>() ?? true;
            parameters.Remove("Enable");
            var kind = pair.Key == "DoubleJump" ? "double_jump" : pair.Key.ToLowerInvariant();
            document.Abilities.Add(new()
            {
                InternalName = kind, Kind = kind, DisplayName = pair.Key, Enabled = enabled,
                Parameters = JsonSerializer.SerializeToElement(parameters)
            });
        }
        document.DefaultClass = document.Classes.First(item => item.Enabled && item.Kind == "zombie").InternalName;
        document.NemesisClass = document.Classes.First(item => item.Enabled && item.Kind == "nemesis").InternalName;
        document.Validate();
        return document;
    }

    private static JsonNode Read(string path)
    {
        if (new FileInfo(path).Length > ZombieCatalogDocument.MaximumBytes)
            throw new InvalidDataException("Старый конфиг превышает 2 МБ");
        return JsonNode.Parse(File.ReadAllText(path), documentOptions: new()
        {
            AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip
        }) ?? throw new InvalidDataException("Пустой старый конфиг");
    }
}
