using System.Text.Json.Serialization;

namespace Menu.Api.Contracts;

/// <summary>
/// Представляет локализуемый текст с обязательным fallback и расширяемым набором переводов.
/// </summary>
/// <remarks>
/// Ключи <see cref="Translations"/> являются нормализованными кодами локалей,
/// например <c>ru</c> или <c>en-US</c>. DTO не проверяет и не изменяет входные строки.
/// </remarks>
public sealed record LocalizedText
{
    /// <summary>Возвращает пустой локализованный текст.</summary>
    public static LocalizedText Empty { get; } = new();

    /// <summary>Fallback-текст, используемый при отсутствии подходящего перевода.</summary>
    [JsonPropertyName("default")]
    public string Default { get; init; } = string.Empty;

    /// <summary>Переводы по коду локали.</summary>
    [JsonPropertyName("translations")]
    public IReadOnlyDictionary<string, string> Translations { get; init; }
        = new Dictionary<string, string>();
}
