using Localization.Core.Application;
using Localization.Core.Configuration;

namespace Localization.Core.Database.Migrations;

internal static class LocalizationSeedEntries
{
    private const string ResourceName = "Localization.Core.Resources.template.jsonc";

    public static Dictionary<string, Dictionary<string, string>> Create()
    {
        using var stream = typeof(LocalizationSeedEntries).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Не найден встроенный ресурс миграций '{ResourceName}'.");

        var config = JsonSerializer.Deserialize<LocalizationFallbackConfig>(
                         stream,
                         new JsonSerializerOptions
                         {
                             PropertyNameCaseInsensitive = true,
                             ReadCommentHandling = JsonCommentHandling.Skip,
                             AllowTrailingCommas = true,
                         })
                     ?? throw new InvalidDataException(
                         "Не удалось прочитать template.jsonc для миграций локализации.");

        LocalizationValidation.ValidateFallback(config);

        return config.Entries.ToDictionary(
            item => item.Key,
            item => new Dictionary<string, string>(
                item.Value,
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }
}
