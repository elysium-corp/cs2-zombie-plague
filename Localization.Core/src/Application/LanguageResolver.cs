using Localization.Api;
using SwiftlyS2.Shared.Players;

namespace Localization.Core.Application;

internal sealed class LanguageResolver(
    LocalizationCache localizationCache,
    PlayerLanguageCache playerLanguageCache) : ILanguageResolver
{
    public string Resolve(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var snapshot = localizationCache.Current ?? EmergencyLocalizationSnapshot.Create();
        playerLanguageCache.TryGetManual(player.SteamID, out var manualLanguage);
        return Resolve(manualLanguage, player.PlayerLanguage.Value, snapshot);
    }

    internal static string Resolve(
        string? manualLanguage,
        string? clientLanguage,
        LocalizationSnapshot snapshot)
    {
        var manual = LocaleNormalizer.Normalize(manualLanguage);
        if (snapshot.IsLanguageEnabled(manual))
        {
            return manual;
        }

        var client = LocaleNormalizer.Normalize(clientLanguage);
        if (snapshot.IsLanguageEnabled(client))
        {
            return client;
        }

        return snapshot.Settings.ServerFallbackLanguage;
    }
}
