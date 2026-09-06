using System.Text.Json;
using SwiftlyS2.Shared;

namespace ZombiePlague.Core.Catalog;

internal static class CatalogFallback
{
    public static async Task<ZombieCatalogDocument> ReadAsync(ISwiftlyCore core, CancellationToken token)
    {
        var directory = core.Configuration.BasePath;
        var path = Path.Combine(directory, "zombie_catalog.json");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(directory);
            var legacy = LegacyZombieCatalog.TryRead(directory);
            if (legacy is not null)
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(legacy, ZombieCatalogDocument.JsonOptions), token).ConfigureAwait(false);
                return legacy;
            }
            var template = await File.ReadAllTextAsync(Path.Combine(core.PluginPath, "resources", "templates", "zombie_catalog.example.json"), token).ConfigureAwait(false);
            // Человеческий конфиг мог существовать отдельно от старого каталога зомби
            var document = JsonSerializer.Deserialize<ZombieCatalogDocument>(template, ZombieCatalogDocument.JsonOptions)!;
            if (File.Exists(Path.Combine(directory, "human_class.json")))
            {
                document.Classes.RemoveAll(item => item.Kind is "human" or "survivor");
                document.FormatVersion = 1;
                CatalogUpgrade.Apply(document, LegacyZombieCatalog.ReadHumans(directory));
            }
            document.Validate();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions), token).ConfigureAwait(false);
            return document;
        }
        if (new FileInfo(path).Length > ZombieCatalogDocument.MaximumBytes)
            throw new InvalidDataException("Fallback классов превышает 2 МБ");
        var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<ZombieCatalogDocument>(json, ZombieCatalogDocument.JsonOptions)!;
        // Исправный v2 не зависит от оставшихся рядом старых файлов
        CatalogUpgrade.Apply(parsed, parsed.FormatVersion == 1 ? LegacyZombieCatalog.ReadHumans(directory) : new());
        parsed.Validate();
        return parsed;
    }
}
