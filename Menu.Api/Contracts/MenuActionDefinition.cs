using System.Text.Json;
using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Contracts;

/// <summary>
/// Описывает безопасное действие пункта или кнопки меню.
/// </summary>
/// <remarks>
/// Произвольная серверная команда намеренно отсутствует в API v1.
/// </remarks>
public sealed record MenuActionDefinition
{
    /// <summary>Тип действия.</summary>
    [JsonPropertyName("kind")]
    public MenuActionKind Kind { get; init; } = MenuActionKind.None;

    /// <summary>Целевое меню для OpenMenu или OpenProviderMenu.</summary>
    [JsonPropertyName("targetMenu")]
    public MenuReferenceDefinition? TargetMenu { get; init; }

    /// <summary>Ключ Provider для ProviderAction.</summary>
    [JsonPropertyName("providerKey")]
    public string? ProviderKey { get; init; }

    /// <summary>Ключ зарегистрированного Provider Action.</summary>
    [JsonPropertyName("providerActionKey")]
    public string? ProviderActionKey { get; init; }

    /// <summary>
    /// JSON-объект аргументов, проверяемый schema и Provider validator.
    /// Для OnChange runtime дополнительно записывает текущее значение в поле <c>value</c>.
    /// </summary>
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }

    /// <summary>Поведение при выгруженном целевом Provider.</summary>
    [JsonPropertyName("providerUnavailableBehavior")]
    public ProviderUnavailableBehavior ProviderUnavailableBehavior { get; init; }
        = ProviderUnavailableBehavior.Disable;
}
