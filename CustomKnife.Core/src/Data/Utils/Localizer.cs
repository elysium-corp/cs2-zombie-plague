using Common.Di;
using Localization.Api;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Utils;

public static class Localizer
{
    public static string GetLocalizeString(IPlayer player, string key)
    {
        var localization = DependencyResolver.GetRequiredService<ILocalizationApi>();
        var localizationKey = $"CustomKnife.{LocalizationKey.Canonicalize(key)}";
        return localization.GetForPlayer(player, localizationKey) ?? localizationKey;
    }
}
