using SwiftlyS2.Shared.Players;

namespace Localization.Api;

/// <summary>
/// Предоставляет локализованные строки из единого memory snapshot ElysiumLocalization.
/// </summary>
/// <remarks>
/// Все методы чтения работают только с памятью и не выполняют SQL-запросы.
/// </remarks>
public interface ILocalizationApi : ILanguageResolver
{
    /// <summary>
    /// Возвращает строку для эффективного языка игрока с fallback на язык сервера.
    /// </summary>
    /// <param name="player">Получатель локализованной строки.</param>
    /// <param name="key">Уникальный ключ локализации.</param>
    /// <param name="placeholders">Значения placeholder без фигурных скобок.</param>
    /// <returns>Готовая строка либо <c>null</c>, если ключ отсутствует и в fallback-языке.</returns>
    string? GetForPlayer(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null);

    /// <summary>
    /// Возвращает строку для явно указанного языка с fallback на язык сервера.
    /// </summary>
    /// <param name="languageCode">Запрошенный код языка.</param>
    /// <param name="key">Уникальный ключ локализации.</param>
    /// <param name="placeholders">Значения placeholder без фигурных скобок.</param>
    /// <returns>Готовая строка либо <c>null</c>, если ключ отсутствует и в fallback-языке.</returns>
    string? GetForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null);

    /// <summary>
    /// Форматирует строку для эффективного языка игрока с проверкой схемы параметров ключа.
    /// </summary>
    /// <param name="player">Получатель локализованной строки.</param>
    /// <param name="key">Уникальный ключ локализации.</param>
    /// <param name="parameters">Типизированные значения параметров без фигурных скобок.</param>
    /// <returns>Готовая строка либо <c>null</c>, если ключ, перевод или обязательный параметр отсутствует.</returns>
    string? FormatForPlayer(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Форматирует строку для указанного языка с проверкой схемы параметров ключа.
    /// </summary>
    /// <param name="languageCode">Запрошенный код языка.</param>
    /// <param name="key">Уникальный ключ локализации.</param>
    /// <param name="parameters">Типизированные значения параметров без фигурных скобок.</param>
    /// <returns>Готовая строка либо <c>null</c>, если ключ, перевод или обязательный параметр отсутствует.</returns>
    string? FormatForLanguage(
        string languageCode,
        string key,
        IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Возвращает схему параметров указанного ключа.
    /// </summary>
    /// <param name="key">Уникальный ключ локализации.</param>
    /// <returns>Неизменяемый список параметров или пустой список для неизвестного ключа.</returns>
    IReadOnlyList<LocalizationParameterDefinition> GetParameterDefinitions(string key);

    /// <summary>Форматирует сообщение с явным режимом разметки и ролью получателя; язык можно переопределить для Preview.</summary>
    /// <param name="player">Получатель, чья активная роль определяет динамический цвет.</param>
    /// <param name="key">Ключ перевода.</param>
    /// <param name="parameters">Исходные, не экранированные значения параметров.</param>
    /// <param name="mode">Формат результата.</param>
    /// <param name="languageCode">Язык Preview; null использует язык игрока.</param>
    /// <returns>Сообщение или null при ошибке параметров либо отсутствии перевода.</returns>
    string? FormatForPlayer(IPlayer player, string key, IReadOnlyDictionary<string, object?> parameters,
        LocalizationOutputMode mode, string? languageCode = null)
    {
        var values = mode == LocalizationOutputMode.Html
            ? parameters.ToDictionary(x => x.Key, x => x.Value is string text
                ? (object?)System.Net.WebUtility.HtmlEncode(text).Replace("[", "&#91;").Replace("]", "&#93;") : x.Value)
            : parameters;
        return languageCode is null ? FormatForPlayer(player, key, values) : FormatForLanguage(languageCode, key, values);
    }

    /// <summary>Возвращает оформление активной роли из памяти; при отсутствии Admin API — обычные цвета.</summary>
    /// <param name="player">Получатель сообщения.</param>
    /// <returns>Оформление роли без запросов в БД.</returns>
    LocalizationPlayerStyle GetPlayerStyle(IPlayer player) => new();

    /// <summary>
    /// Возвращает включённый тег для эффективного языка игрока.
    /// </summary>
    /// <param name="player">Получатель локализованного тега.</param>
    /// <param name="tagKey">Стабильный ключ тега без префикса <c>Tag.</c>.</param>
    /// <returns>Локализованный тег либо <c>null</c>, если тег отключён, отсутствует или не переведён.</returns>
    LocalizationTag? GetTagForPlayer(IPlayer player, string tagKey);

    /// <summary>
    /// Возвращает включённый тег для явно указанного языка.
    /// </summary>
    /// <param name="languageCode">Запрошенный код языка.</param>
    /// <param name="tagKey">Стабильный ключ тега без префикса <c>Tag.</c>.</param>
    /// <returns>Локализованный тег либо <c>null</c>, если тег отключён, отсутствует или не переведён.</returns>
    LocalizationTag? GetTagForLanguage(string languageCode, string tagKey);

    /// <summary>
    /// Возвращает включённые языки в порядке, заданном администратором.
    /// </summary>
    /// <returns>Неизменяемый снимок доступных языков.</returns>
    IReadOnlyList<LocalizationLanguage> GetEnabledLanguages();

    /// <summary>
    /// Возвращает текущий fallback-язык сервера.
    /// </summary>
    string ServerFallbackLanguage { get; }

    /// <summary>
    /// Ключ Shared Interface.
    /// </summary>
    public new static readonly string SharedApiKey = "Localization.Api.ILocalizationApi";
}
