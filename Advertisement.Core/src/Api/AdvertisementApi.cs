using Advertisement.Api;
using Advertisement.Core.Application;
using Advertisement.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Api;

internal sealed class AdvertisementApi(
    ISwiftlyCore core,
    AdvertisementCache cache,
    PlayerLocaleResolver localeResolver,
    AdvertisementScheduler scheduler) : IAdvertisementApi
{
    public string GetPlayerLocale(IPlayer player)
    {
        var snapshot = cache.Current;
        if (snapshot is not null)
        {
            return localeResolver.Resolve(player, snapshot.Settings);
        }

        var engineLocale = LocaleNormalizer.Normalize(player.PlayerLanguage.Value);
        return string.IsNullOrWhiteSpace(engineLocale) ? "ru" : engineLocale;
    }

    public string? GetText(string messageKey, string locale)
    {
        var snapshot = cache.Current;
        var message = FindMessage(snapshot, messageKey);
        if (snapshot is null || message is null)
        {
            return null;
        }

        return ResolveTranslation(
            message.Translations,
            LocaleNormalizer.Normalize(locale),
            snapshot.Settings.DefaultLocale);
    }

    public string? GetText(string messageKey, IPlayer player)
    {
        return GetText(messageKey, GetPlayerLocale(player));
    }

    public bool Send(IPlayer player, string messageKey, string? tagKey = null)
    {
        if (player is not { IsAuthorized: true, IsFakeClient: false })
        {
            return false;
        }

        var message = FindMessage(cache.Current, messageKey);
        return message is { Enabled: true }
               && scheduler.SendManual(message, [player], NormalizeTagKey(tagKey));
    }

    public int SendToAll(string messageKey, string? tagKey = null)
    {
        var message = FindMessage(cache.Current, messageKey);
        if (message is not { Enabled: true })
        {
            return 0;
        }

        var players = core.PlayerManager.GetAllPlayers()
            .Where(player => player is { IsAuthorized: true, IsFakeClient: false })
            .ToArray();
        return players.Length > 0 && scheduler.SendManual(message, players, NormalizeTagKey(tagKey))
            ? players.Length
            : 0;
    }

    private static AdvertisementMessage? FindMessage(AdvertisementSnapshot? snapshot, string messageKey)
    {
        if (snapshot is null || string.IsNullOrWhiteSpace(messageKey))
        {
            return null;
        }

        return snapshot.Messages.Values.FirstOrDefault(message =>
            message.Key.Equals(messageKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveTranslation(
        FrozenDictionary<string, string> translations,
        string locale,
        string fallback)
    {
        return translations.TryGetValue(locale, out var value)
            ? value
            : translations.GetValueOrDefault(fallback);
    }

    private static string? NormalizeTagKey(string? tagKey)
    {
        return tagKey is null ? null : tagKey.Trim();
    }
}
