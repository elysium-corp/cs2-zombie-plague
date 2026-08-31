using SwiftlyS2.Shared.Players;

namespace Localization.Api;

/// <summary>
/// Содержит безопасные способы вывода ключей, обязательных для интерфейса игрока.
/// </summary>
public static class LocalizationApiExtensions
{
    /// <summary>
    /// Возвращает локализованный текст или сам ключ, если каталог ещё не содержит перевод.
    /// </summary>
    /// <param name="localization">Общий API локализации.</param>
    /// <param name="player">Получатель текста.</param>
    /// <param name="key">Ключ локализации.</param>
    /// <param name="placeholders">Значения placeholder без фигурных скобок.</param>
    /// <returns>Локализованный текст или переданный ключ.</returns>
    public static string GetForPlayerOrKey(
        this ILocalizationApi localization,
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null)
    {
        return localization.GetForPlayer(player, key, placeholders) ?? key;
    }

    /// <summary>
    /// Форматирует локализованный текст типизированными параметрами или возвращает ключ при ошибке контракта.
    /// </summary>
    /// <param name="localization">Общий API локализации.</param>
    /// <param name="player">Получатель текста.</param>
    /// <param name="key">Ключ локализации.</param>
    /// <param name="parameters">Типизированные значения параметров без фигурных скобок.</param>
    /// <returns>Локализованный текст или переданный ключ.</returns>
    public static string FormatForPlayerOrKey(
        this ILocalizationApi localization,
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, object?> parameters)
    {
        return localization.FormatForPlayer(player, key, parameters) ?? key;
    }
}
