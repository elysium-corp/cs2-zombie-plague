using Localization.Core.Application;
using Localization.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Localization.Core.Data;

internal sealed class FallbackLocalizationProvider(IOptionsMonitor<LocalizationFallbackConfig> options)
{
    public LocalizationSnapshot Load() => Load(options.CurrentValue);

    internal static LocalizationSnapshot Load(LocalizationFallbackConfig config)
    {
        if (IsUnconfiguredDefault(config))
        {
            return EmergencyLocalizationSnapshot.Create();
        }

        LocalizationValidation.ValidateFallback(config);
        return Build(config, LocalizationSource.Config);
    }

    private static bool IsUnconfiguredDefault(LocalizationFallbackConfig config)
    {
        return config.SchemaVersion == LocalizationValidation.SupportedSchemaVersion
            && config.Version == 0
            && config.GeneratedAt == DateTimeOffset.UnixEpoch
            && string.IsNullOrWhiteSpace(config.Checksum)
            && string.Equals(config.ServerFallbackLanguage, "ru", StringComparison.OrdinalIgnoreCase)
            && config.Languages.SequenceEqual(["ru", "en", "de", "pl"], StringComparer.OrdinalIgnoreCase)
            && config.RefreshIntervalSeconds == 30
            && config.LocalCacheEnabled
            && config.LogMissingKeys
            && config.Entries.Count == 0;
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

        var mergedEntries = BuiltInLocalizationEntries.Create();
        foreach (var (key, translations) in config.Entries)
        {
            mergedEntries[key] = translations;
        }

        long entryId = -1;
        var entries = mergedEntries
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
            ["Equipment.LaserMine.AlreadyOwned"] = RuEn("У вас уже есть лазерная мина", "You already have a laser mine"),
            ["Equipment.LaserMine.Granted"] = RuEn("Лазерная мина получена", "Laser mine granted"),
            ["Equipment.LaserMine.Installing"] = RuEn("Установка лазерной мины", "Installing laser mine"),
            ["Equipment.LaserMine.InvalidSurface"] = RuEn("Здесь нельзя установить лазерную мину", "The laser mine cannot be placed here"),
            ["Equipment.Item.custom_equipment.laser_mine.Name"] = RuEn("Лазерная мина", "Laser Mine"),
            ["Equipment.Item.custom_equipment.shake_nade.Name"] = RuEn("Сотрясающая граната", "Shake Nade"),
            ["Equipment.Item.custom_equipment.jump_nade.Name"] = RuEn("Прыжковая граната", "Jump Nade"),
            ["Equipment.Item.custom_equipment.barrier_nade.Name"] = RuEn("Барьерная граната", "Barrier Nade"),
            ["Equipment.Item.custom_equipment.frost_nade.Name"] = RuEn("Замораживающая граната", "Frost Nade"),
            ["Equipment.Item.custom_equipment.fire_nade.Name"] = RuEn("Огненная граната", "Fire Nade"),
            ["Ammo.Warning.NotEnoughMoney"] = RuEn("Не хватает денег", "You don't have enough money"),
            ["Ammo.Warning.EnoughAmmo"] = RuEn("Боезапас заполнен", "Ammo full"),
            ["Admin.Menu.Title"] = RuEn("Админ-меню", "Admin menu"),
            ["Admin.Menu.Kick"] = RuEn("Кикнуть игрока", "Kick player"),
            ["Admin.Menu.Ban"] = RuEn("Заблокировать игрока", "Ban player"),
            ["Admin.Menu.Kill"] = RuEn("Убить игрока", "Kill player"),
            ["Admin.Menu.Respawn"] = RuEn("Возродить игрока", "Respawn player"),
            ["Admin.Menu.Round"] = RuEn("Управление раундом", "Round management"),
            ["Admin.Kick.Title"] = RuEn("Кикнуть игрока", "Kick player"),
            ["Admin.Kick.Reason"] = RuEn("Вы были исключены администратором {administrator}", "You were kicked by administrator {administrator}"),
            ["Admin.Kill.Title"] = RuEn("Убить игрока", "Kill player"),
            ["Admin.Respawn.Title"] = RuEn("Возродить игрока", "Respawn player"),
            ["Admin.Round.Title"] = RuEn("Управление раундом", "Round management"),
            ["Admin.Round.EndWarmup"] = RuEn("Завершить разминку", "End warmup"),
            ["Admin.Round.EndRound"] = RuEn("Завершить раунд", "End round"),
            ["Admin.Round.Restart"] = RuEn("Перезапустить игру", "Restart game"),
            ["Admin.Ban.Title"] = RuEn("Заблокировать игрока", "Ban player"),
            ["Admin.Ban.DurationTitle"] = RuEn("Срок блокировки: {player}", "Ban duration: {player}"),
            ["Admin.Ban.Duration.30Minutes"] = RuEn("30 минут", "30 minutes"),
            ["Admin.Ban.Duration.1Hour"] = RuEn("1 час", "1 hour"),
            ["Admin.Ban.Duration.6Hours"] = RuEn("6 часов", "6 hours"),
            ["Admin.Ban.Duration.1Day"] = RuEn("1 день", "1 day"),
            ["Admin.Ban.Duration.7Days"] = RuEn("7 дней", "7 days"),
            ["Admin.Ban.Duration.30Days"] = RuEn("30 дней", "30 days"),
            ["Admin.Ban.Duration.Permanent"] = RuEn("Навсегда", "Permanent"),
            ["Admin.Ban.ReasonTitle"] = RuEn("Причина блокировки: {player}", "Ban reason: {player}"),
            ["Admin.Ban.ChoosePredefinedReason"] = RuEn("Выбрать причину", "Choose a reason"),
            ["Admin.Ban.EnterCustomReason"] = RuEn("Ввести свою причину", "Enter custom reason"),
            ["Admin.Ban.CustomReasonInput"] = RuEn("Причина", "Reason"),
            ["Admin.Ban.CustomReasonHint"] = RuEn("Введите причину блокировки", "Enter the ban reason"),
            ["Admin.Ban.Reason.Cheating"] = RuEn("Использование читов", "Cheating"),
            ["Admin.Ban.Reason.Toxicity"] = RuEn("Оскорбления и токсичность", "Toxicity and abuse"),
            ["Admin.Ban.Reason.Spam"] = RuEn("Спам", "Spam"),
            ["Admin.Ban.Reason.GameplayInterference"] = RuEn("Помеха игровому процессу", "Gameplay interference"),
            ["Admin.Ban.Reason.Evasion"] = RuEn("Обход наказания", "Ban evasion"),
            ["Admin.Ban.Reason.Advertising"] = RuEn("Сторонняя реклама", "Unauthorized advertising"),
            ["Admin.Ban.Reason.RulesViolation"] = RuEn("Нарушение правил сервера", "Server rules violation"),
            ["Admin.Ban.KickPermanent"] = RuEn("Вы заблокированы навсегда. Причина: {reason}", "You are permanently banned. Reason: {reason}"),
            ["Admin.Ban.KickTemporary"] = RuEn("Вы заблокированы до {expires_at}. Причина: {reason}", "You are banned until {expires_at}. Reason: {reason}"),
            ["SupplyBox.Editor.Title"] = RuEn("Редактор ящиков снабжения", "Supply box editor"),
            ["SupplyBox.Editor.RotateRight"] = RuEn("Повернуть вправо", "Rotate right"),
            ["SupplyBox.Editor.RotateLeft"] = RuEn("Повернуть влево", "Rotate left"),
            ["SupplyBox.Editor.Cancel"] = RuEn("Отмена", "Cancel"),
            ["SupplyBox.Editor.Install"] = RuEn("Установить", "Install"),
            ["SupplyBox.Editor.AddTitle"] = RuEn("Новый ящик снабжения", "New supply box"),
            ["SupplyBox.Editor.RemoveTitle"] = RuEn("Удаление ящика", "Remove supply box"),
            ["SupplyBox.Editor.RemoveItem"] = RuEn("Ящик №{index}", "Box #{index}"),
            ["SupplyBox.Editor.Create"] = RuEn("Добавить ящик", "Add box"),
            ["SupplyBox.Editor.Remove"] = RuEn("Удалить ящик", "Remove box"),
            ["ZombiePlague.Admin.Title"] = RuEn("Админка Zombie Mode", "Zombie Mode admin"),
            ["ZombiePlague.Admin.RootItem"] = RuEn("[Zombie Mode] Админ-меню", "[Zombie Mode] Admin menu"),
            ["ZombiePlague.Admin.Infect"] = RuEn("Заразить игрока", "Infect player"),
            ["ZombiePlague.Admin.Disinfect"] = RuEn("Вылечить игрока", "Disinfect player"),
            ["ZombiePlague.Admin.Rounds"] = RuEn("Управление раундами", "Round management"),
            ["ZombiePlague.Admin.Infect.Title"] = RuEn("Сделать зомби", "Turn into zombie"),
            ["ZombiePlague.Admin.Disinfect.Title"] = RuEn("Сделать человеком", "Turn into human"),
            ["ZombiePlague.Admin.Round.Title"] = RuEn("Управление раундами", "Round management"),
            ["ZombiePlague.Admin.Round.Current"] = RuEn("Текущий:", "Current:"),
            ["ZombiePlague.Admin.Round.Next"] = RuEn("Следующий:", "Next:"),
            ["ZombiePlague.Admin.Round.Automatic"] = RuEn("Автоматически", "Automatic"),
            ["ZombiePlague.Admin.Round.State.Preparing"] = RuEn("Подготовка", "Preparing"),
            ["ZombiePlague.Admin.Round.State.None"] = RuEn("Нет", "None"),
            ["ZombiePlague.Admin.Round.State.Unknown"] = RuEn("Неизвестно", "Unknown"),
            ["ZombiePlague.Admin.Round.StartNow"] = RuEn("⚡ Запустить немедленно", "⚡ Start immediately"),
            ["ZombiePlague.Admin.Round.SelectNext"] = RuEn("Выбрать следующий раунд", "Select next round"),
            ["ZombiePlague.Admin.Round.Started"] = RuEn("Запущен раунд: {round}", "Round started: {round}"),
            ["ZombiePlague.Admin.Round.NotPreparing"] = RuEn("Подготовка уже завершена", "Preparation has already ended"),
            ["ZombiePlague.Admin.Round.CannotStart"] = RuEn("Не удалось подобрать раунд для запуска", "No eligible round could be selected"),
            ["ZombiePlague.Admin.Round.Cancelled"] = RuEn("Запуск раунда отменён", "Round start was cancelled"),
            ["ZombiePlague.Admin.Round.Selection.Title"] = RuEn("Следующий раунд", "Next round"),
            ["ZombiePlague.Admin.Round.Selection.AutomaticSelected"] = RuEn("Следующий раунд будет выбран автоматически.", "The next round will be selected automatically."),
            ["ZombiePlague.Admin.Round.Selection.Selected"] = RuEn("Следующий раунд: {round}", "Next round: {round}"),
            ["ZombiePlague.Admin.Round.Selection.ConditionsPending"] = RuEn("Следующий раунд: {round}. Условия будут проверены перед запуском.", "Next round: {round}. Its conditions will be checked before start."),
            ["ZombiePlague.Round.infection.Name"] = RuEn("Инфекция", "Infection"),
            ["ZombiePlague.Round.plague.Name"] = RuEn("Чума", "Plague"),
            ["ZombiePlague.Round.nemesis.Name"] = RuEn("Немезида", "Nemesis"),
            ["ZombiePlague.Round.survivor.Name"] = RuEn("Выживший", "Survivor"),
            ["ZombiePlague.Round.Preparing"] = RuEn("До заражения {seconds} сек.", "Infection begins in {seconds} sec."),
            ["ZombiePlague.Round.Infection.FirstInfected"] = RuEn("Первый заражённый — {player}", "First infected — {player}"),
            ["ZombiePlague.Round.Plague.Started"] = RuEn("Массовое заражение!", "Mass infection!"),
            ["ZombiePlague.Round.Nemesis.Selected"] = RuEn("Немезида — {player}", "Nemesis — {player}"),
            ["ZombiePlague.Round.Survivor.Selected"] = RuEn("Выживший — {player}", "Survivor — {player}"),
            ["ZombiePlague.Ability.Cooldown"] = RuEn("Способность восстановится через {seconds} сек.", "Ability ready in {seconds} sec."),
            ["ZombiePlague.ZClass.zombie_cleric.Name"] = RuEn("Клирик", "Cleric"),
            ["ZombiePlague.ZClass.zombie_cleric.Description"] = RuEn("Лечит зомби", "Heals zombies"),
            ["ZombiePlague.ZClass.zombie_hunter.Name"] = RuEn("Охотник", "Hunter"),
            ["ZombiePlague.ZClass.zombie_hunter.Description"] = RuEn("Устанавливает ловушки", "Places traps"),
            ["ZombiePlague.ZClass.zombie_assassin.Name"] = RuEn("Ассасин", "Assassin"),
            ["ZombiePlague.ZClass.zombie_assassin.Description"] = RuEn("Ускоряется", "Can charge"),
            ["ZombiePlague.ZClass.zombie_heavy.Name"] = RuEn("Тяжёлый", "Heavy"),
            ["ZombiePlague.ZClass.zombie_heavy.Description"] = RuEn("Ослепляет людей", "Blinds humans"),
            ["ZombiePlague.ZClass.zombie_smoker.Name"] = RuEn("Курильщик", "Smoker"),
            ["ZombiePlague.ZClass.zombie_smoker.Description"] = RuEn("Притягивает людей", "Pulls humans"),
            ["ZombiePlague.ZClass.zombie_nemesis.Name"] = RuEn("Немезида", "Nemesis"),
            ["ZombiePlague.ZClass.zombie_nemesis.Description"] = RuEn("Убивает одним ударом", "Kills with one hit"),
            ["CustomKnife.knife_piercer.Name"] = RuEn("Пронзатель", "Piercer"),
            ["CustomKnife.knife_piercer.Description"] = RuEn("Усиленная отдача", "Increased knockback"),
            ["CustomKnife.knife_spike.Name"] = RuEn("Шип", "Spike"),
            ["CustomKnife.knife_spike.Description"] = RuEn("Повышенная скорость", "Increased speed"),
            ["CustomKnife.knife_axe.Name"] = RuEn("Топор", "Axe"),
            ["CustomKnife.knife_axe.Description"] = RuEn("Пониженная гравитация", "Reduced gravity"),
            ["CustomKnife.knife_katana.Name"] = RuEn("Катана", "Katana"),
            ["CustomKnife.knife_katana.Description"] = RuEn("VIP-нож", "VIP knife"),
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
