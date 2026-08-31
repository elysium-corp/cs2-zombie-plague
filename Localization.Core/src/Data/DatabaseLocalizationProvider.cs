using Localization.Core.Application;
using Localization.Core.Database;
using Localization.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Data;

internal sealed class DatabaseLocalizationProvider(
    IDbContextFactory<LocalizationDbContext> contextFactory)
{
    public async Task<LocalizationSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settingsEntity = await context.Settings.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == 1, cancellationToken)
            ?? throw new InvalidOperationException("В localization.settings отсутствует строка id = 1.");

        var languageEntities = await context.Languages.AsNoTracking()
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Id)
            .ToListAsync(cancellationToken);
        var entryEntities = await context.Entries.AsNoTracking()
            .Include(entity => entity.Translations)
            .OrderBy(entity => entity.Key)
            .ToListAsync(cancellationToken);

        var languages = languageEntities
            .Select(MapLanguage)
            .GroupBy(language => language.Code, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                group => group.Key,
                group => group.Last(),
                StringComparer.OrdinalIgnoreCase);
        var entries = entryEntities
            .Select(entity => MapEntry(entity, languages.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase)))
            .ToFrozenDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        var snapshot = new LocalizationSnapshot(
            new LocalizationSettings(
                LocaleNormalizer.Normalize(settingsEntity.ServerFallbackLanguage),
                settingsEntity.RefreshIntervalSeconds,
                settingsEntity.LocalCacheEnabled,
                settingsEntity.LogMissingKeys,
                settingsEntity.ConfigurationVersion),
            languages,
            entries,
            DateTimeOffset.UtcNow,
            LocalizationSource.Database);

        LocalizationValidation.ValidateSnapshot(snapshot);
        return snapshot;
    }

    private static LocalizationLanguageState MapLanguage(LocalizationLanguageEntity entity) => new(
        entity.Id,
        LocaleNormalizer.Normalize(entity.Code),
        entity.Name,
        entity.NativeName,
        entity.Enabled,
        entity.SortOrder);

    private static LocalizationEntry MapEntry(
        LocalizationEntryEntity entity,
        IReadOnlySet<string> languages) => new(
        entity.Id,
        entity.Key,
        entity.IsCritical,
        LocalizationValidation.NormalizeTranslations(
            entity.Translations.Select(translation =>
                new KeyValuePair<string, string>(translation.LanguageCode, translation.Text)),
            languages));
}

internal sealed class PlayerLanguagePreferenceRepository(
    IDbContextFactory<LocalizationDbContext> contextFactory)
{
    public async Task<string?> LoadAsync(ulong steamId, CancellationToken cancellationToken)
    {
        var id = checked((long)steamId);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var language = await context.PlayerPreferences.AsNoTracking()
            .Where(entity => entity.SteamId == id)
            .Select(entity => entity.LanguageCode)
            .SingleOrDefaultAsync(cancellationToken);

        return language is null ? null : LocaleNormalizer.Normalize(language);
    }

    public async Task SaveAsync(
        ulong steamId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var id = checked((long)steamId);
        var normalized = LocaleNormalizer.Normalize(languageCode);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Код языка не заполнен.", nameof(languageCode));
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var languageEnabled = await context.Languages.AsNoTracking()
            .AnyAsync(entity => entity.Code == normalized && entity.Enabled, cancellationToken);
        if (!languageEnabled)
        {
            throw new InvalidOperationException($"Язык '{normalized}' отсутствует или отключён.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = await context.PlayerPreferences
            .SingleOrDefaultAsync(item => item.SteamId == id, cancellationToken);
        if (entity is null)
        {
            context.PlayerPreferences.Add(new PlayerLanguagePreferenceEntity
            {
                SteamId = id,
                LanguageCode = normalized,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            entity.LanguageCode = normalized;
            entity.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
