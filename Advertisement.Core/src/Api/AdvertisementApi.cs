using Advertisement.Api;
using Advertisement.Core.Application;
using Advertisement.Core.Data;
using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Advertisement.Core.Api;

internal sealed class AdvertisementApi(
    ISwiftlyCore core,
    AdvertisementCache cache,
    Func<ILocalizationApi> localization,
    AdvertisementScheduler scheduler) : IAdvertisementApi
{
    [Obsolete("Используйте ILocalizationApi.Resolve(IPlayer). Метод сохранён для совместимости.")]
    public string GetPlayerLocale(IPlayer player) => localization().Resolve(player);

    public string? GetText(string messageKey, string locale)
    {
        var message = FindMessage(cache.Current, messageKey);
        return message is null
            ? null
            : localization().GetForLanguage(locale, message.LocalizationKey);
    }

    public string? GetText(string messageKey, IPlayer player)
    {
        var message = FindMessage(cache.Current, messageKey);
        return message is null
            ? null
            : localization().GetForPlayer(player, message.LocalizationKey);
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

    private static string? NormalizeTagKey(string? tagKey)
    {
        return tagKey is null ? null : tagKey.Trim();
    }
}
