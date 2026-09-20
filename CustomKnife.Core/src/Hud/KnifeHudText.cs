using System.Globalization;
using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using Localization.Api;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Hud;

internal sealed class KnifeHudText(ILocalizationApi localization)
{
    public string Get(IPlayer player, string suffix, string fallback,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        var translated = localization.GetForPlayer(player, "Menu.Knife.Hud." + suffix, parameters);
        if (translated is not null) return translated;
        foreach (var pair in parameters ?? new Dictionary<string, string>())
            fallback = fallback.Replace("{" + pair.Key + "}", pair.Value, StringComparison.Ordinal);
        return fallback;
    }

    public string Name(IPlayer player, IKnife knife) => Field(player, knife, "Name", knife.DisplayName);
    public string Description(IPlayer player, IKnife knife) => Field(player, knife, "Description", knife.Description);
    public string? Custom(IPlayer player, string? key) => string.IsNullOrWhiteSpace(key) ? null : localization.GetForPlayer(player, key);

    public CultureInfo Culture(IPlayer player)
    {
        var language = localization.Resolve(player);
        if (string.IsNullOrWhiteSpace(language)) return CultureInfo.InvariantCulture;
        try { return CultureInfo.GetCultureInfo(language); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    private string Field(IPlayer player, IKnife knife, string field, string fallback)
    {
        var key = knife is ILocalizedKnife localized
            ? field == "Name" ? localized.DisplayNameKey : localized.DescriptionKey
            : $"CustomKnife.{LocalizationKey.Canonicalize(knife.InternalName)}.{field}";
        return localization.GetForPlayer(player, key) ?? fallback;
    }
}
