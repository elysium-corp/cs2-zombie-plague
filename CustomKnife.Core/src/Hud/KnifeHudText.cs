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

    public string[] Benefits(IPlayer player, IKnife knife, KnifeHudAppearance appearance)
    {
        var custom = (appearance.BenefitKeys ?? []).Take(4).Select(key => Custom(player, key)).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (custom.Length > 0) return custom.Select(value => value!).ToArray();
        return
        [
            Stat("Speed", "Speed: {value}", knife.Speed),
            Stat("Damage", "Damage: ×{value}", knife.DamageMultiplier),
            Stat("Gravity", "Gravity: {value}", knife.Gravity),
            Stat("Knockback", "Knockback: {value}", knife.KnockbackData.Recoil)
        ];
        string Stat(string key, string fallback, float value) => Get(player, "Stat." + key, fallback,
            new Dictionary<string, string> { ["value"] = value.ToString("0.##", CultureInfo.InvariantCulture) });
    }

    private string Field(IPlayer player, IKnife knife, string field, string fallback)
    {
        var key = knife is ILocalizedKnife localized
            ? field == "Name" ? localized.DisplayNameKey : localized.DescriptionKey
            : $"CustomKnife.{LocalizationKey.Canonicalize(knife.InternalName)}.{field}";
        return localization.GetForPlayer(player, key) ?? fallback;
    }
}
