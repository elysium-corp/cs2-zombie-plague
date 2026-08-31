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
