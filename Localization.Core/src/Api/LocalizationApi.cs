using Localization.Api;
using Localization.Core.Application;
using SwiftlyS2.Shared.Players;

namespace Localization.Core.Api;

internal sealed class LocalizationApi(
    LocalizationCache cache,
    LanguageResolver languageResolver,
    LocalizationRuntime runtime) : ILocalizationApi
{
    public string ServerFallbackLanguage =>
        cache.Current?.Settings.ServerFallbackLanguage ?? "ru";

    public string Resolve(IPlayer player) => languageResolver.Resolve(player);

    public string? GetForPlayer(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null) =>
        runtime.GetForPlayer(player, key, placeholders);

    public string? GetForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null) =>
        runtime.GetForLanguage(languageCode, key, placeholders);

    public IReadOnlyList<LocalizationLanguage> GetEnabledLanguages()
    {
        var languages = cache.Current?.Languages.Values.AsEnumerable()
                        ?? Enumerable.Empty<LocalizationLanguageState>();

        return languages
            .Where(language => language.Enabled)
            .OrderBy(language => language.SortOrder)
            .ThenBy(language => language.Id)
            .Select(language => language.ToContract())
            .ToArray();
    }
}
