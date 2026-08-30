using Localization.Core.Application;
using Localization.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Localization.Core.Data;

internal sealed class FallbackLocalizationProvider(IOptionsMonitor<LocalizationFallbackConfig> options)
{
    public LocalizationSnapshot Load()
    {
        var config = options.CurrentValue;
        LocalizationValidation.ValidateFallback(config);
        return Build(config, LocalizationSource.Config);
    }

    internal static LocalizationSnapshot Build(
        LocalizationFallbackConfig config,
        LocalizationSource source)
    {
        var orderedLanguageCodes = config.Languages
            .Select(LocaleNormalizer.Normalize)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var languageCodes = LocalizationValidation.NormalizeLanguages(orderedLanguageCodes);
        var languages = orderedLanguageCodes
            .Select((code, index) => LanguageNames.Create(-(index + 1L), code, index))
            .ToFrozenDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);

        long entryId = -1;
        var entries = config.Entries
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                item => item.Key,
                item => new LocalizationEntry(
                    entryId--,
                    item.Key,
                    LocalizationValidation.CriticalKeys.Contains(item.Key),
                    LocalizationValidation.NormalizeTranslations(item.Value, languageCodes)),
                StringComparer.OrdinalIgnoreCase);

        var snapshot = new LocalizationSnapshot(
            new LocalizationSettings(
                LocaleNormalizer.Normalize(config.ServerFallbackLanguage),
                Math.Max(5, config.RefreshIntervalSeconds),
                config.LocalCacheEnabled,
                config.LogMissingKeys,
                config.Version),
            languages,
            entries,
            DateTimeOffset.UtcNow,
            source);

        LocalizationValidation.ValidateSnapshot(snapshot);
        return snapshot;
    }
}

internal static class EmergencyLocalizationSnapshot
{
    public static LocalizationSnapshot Create()
    {
        var config = new LocalizationFallbackConfig
        {
            Version = 1,
            GeneratedAt = DateTimeOffset.UtcNow,
            ServerFallbackLanguage = "ru",
            Languages = ["ru", "en", "de", "pl"],
            Entries = BuiltInLocalizationEntries.Create(),
        };

        return FallbackLocalizationProvider.Build(config, LocalizationSource.Emergency);
    }
}

internal static class LanguageNames
{
    public static LocalizationLanguageState Create(long id, string code, int sortOrder)
    {
        var normalized = LocaleNormalizer.Normalize(code);
        var (name, nativeName) = normalized switch
        {
            "ru" => ("Русский", "Русский"),
            "en" => ("English", "English"),
            "de" => ("Deutsch", "Deutsch"),
            "pl" => ("Polski", "Polski"),
            "uk" => ("Украинский", "Українська"),
            "pt-BR" => ("Português (Brasil)", "Português (Brasil)"),
            "zh-CN" => ("Chinese (Simplified)", "简体中文"),
            "zh-TW" => ("Chinese (Traditional)", "繁體中文"),
            _ => (normalized.ToUpperInvariant(), normalized.ToUpperInvariant()),
        };

        return new LocalizationLanguageState(id, normalized, name, nativeName, true, sortOrder);
    }
}

internal static class BuiltInLocalizationEntries
{
    public static Dictionary<string, Dictionary<string, string>> Create() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["localization.menu.title"] = Translations(
                "Язык / Language", "Language", "Sprache", "Język"),
            ["localization.menu.changed"] = Translations(
                "Язык изменён на {language}",
                "Language changed to {language}",
                "Sprache geändert zu {language}",
                "Zmieniono język na {language}"),
            ["localization.menu.loading"] = Translations(
                "Локализация ещё загружается",
                "Localization is still loading",
                "Die Lokalisierung wird noch geladen",
                "Lokalizacja wciąż się ładuje"),
            ["localization.menu.unavailable"] = Translations(
                "Не удалось изменить язык. Попробуйте позже",
                "Unable to change language. Try again later",
                "Die Sprache konnte nicht geändert werden. Versuche es später erneut",
                "Nie udało się zmienić języka. Spróbuj ponownie później"),
            ["advertisement.tags.elysium"] = RuEn("Elysium", "Elysium"),
            ["advertisement.messages.discord"] = RuEn(
                "Наш Discord: {accent}discord.gg/elysium{/accent}",
                "Our Discord: {accent}discord.gg/elysium{/accent}"),
            ["ResetScore.ResetMessage"] = RuEn("Ваш счёт обнулён!", "Your score has been reset!"),
            ["DamageNotify.HitMessage"] = RuEn("Вы попали в", "You hit"),
            ["Statistics.PointsGained"] = RuEn(
                "За прошлый раунд вы получили +{points} очков.",
                "You gained +{points} points last round."),
            ["Statistics.PointsLost"] = RuEn(
                "За прошлый раунд вы потеряли {points} очков.",
                "You lost {points} points last round."),
            ["Statistics.PointsUnchanged"] = RuEn(
                "За прошлый раунд ваши очки не изменились.",
                "Your points did not change last round."),
            ["Menu.Main.Title"] = RuEn("Меню сервера", "Server menu"),
            ["Menu.Main.Item.ZClass.Title"] = RuEn("Выбрать класс зомби", "Select zombie class"),
            ["Menu.ZClass.Title"] = RuEn("Классы зомби", "Zombie classes"),
            ["Menu.ZClass.Selected"] = RuEn("{class} [выбран]", "{class} [selected]"),
            ["Menu.ZClass.SelectionSuccess"] = RuEn(
                "Вы успешно выбрали класс зомби: {class}",
                "Zombie class selected: {class}"),
            ["Menu.Main.Item.Knife.Title"] = RuEn("Выбрать нож", "Select knife"),
            ["Menu.Knife.Title"] = RuEn("Ножи", "Knives"),
            ["Menu.Knife.Selected"] = RuEn("{knife} [выбран]", "{knife} [selected]"),
            ["Menu.Knife.SelectionSuccess"] = RuEn("Вы выбрали нож: {knife}", "Knife selected: {knife}"),
            ["CustomKnife.Monarch.Name"] = RuEn("Монарх", "Monarch"),
            ["Menu.Main.Item.Equipment.Title"] = RuEn("Магазин снаряжения", "Equipment Shop"),
            ["Menu.Equipment.Title"] = RuEn("Снаряжение", "Equipment"),
            ["Menu.Equipment.Category.Pistol"] = RuEn("Пистолеты", "Pistols"),
            ["Menu.Equipment.Category.SubmachineGun"] = RuEn("Пистолеты-пулемёты", "Submachine Guns"),
            ["Menu.Equipment.Category.Rifle"] = RuEn("Штурмовые винтовки", "Rifles"),
            ["Menu.Equipment.Category.Shotgun"] = RuEn("Дробовики", "Shotguns"),
            ["Menu.Equipment.Category.SniperRifle"] = RuEn("Снайперские винтовки", "Sniper Rifles"),
            ["Menu.Equipment.Category.MachineGun"] = RuEn("Пулемёты", "Machine Guns"),
            ["Menu.Equipment.Category.Grenade"] = RuEn("Гранаты", "Grenades"),
            ["Menu.Equipment.Category.Equipment"] = RuEn("Экипировка", "Equipment"),
            ["Equipment.Errors.RoleUnavailable"] = RuEn(
                "Этот предмет недоступен для текущей роли!",
                "This item is unavailable for your current role!"),
            ["Equipment.Errors.NotEnoughMoney"] = RuEn("Недостаточно денег!", "Not enough money!"),
            ["Ammo.Warning.NotEnoughMoney"] = RuEn("Не хватает денег", "You don't have enough money"),
            ["Ammo.Warning.EnoughAmmo"] = RuEn("Боезапас заполнен", "Ammo full"),
            ["RoundRatingNotify.prefix"] = RuEn("[[green]Elysium[default]]", "[[green]Elysium[default]]"),
            ["RoundRatingNotify.HumanTop"] = RuEn(
                "Лучший игрок за людей: {player} — нанёс {value} урона.",
                "Best human player: {player} — dealt {value} damage."),
            ["RoundRatingNotify.ZombieTop"] = RuEn(
                "Лучший игрок за зомби: {player} — заразил {value} игроков.",
                "Best zombie player: {player} — infected {value} players."),
        };

    private static Dictionary<string, string> RuEn(string ru, string en) =>
        new(StringComparer.OrdinalIgnoreCase) { ["ru"] = ru, ["en"] = en };

    private static Dictionary<string, string> Translations(string ru, string en, string de, string pl) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ru"] = ru,
            ["en"] = en,
            ["de"] = de,
            ["pl"] = pl,
        };
}
