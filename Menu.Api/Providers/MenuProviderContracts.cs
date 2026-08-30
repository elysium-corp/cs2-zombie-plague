using System.Text.Json;
using System.Text.Json.Serialization;
using Menu.Api.Contracts;
using Menu.Api.Results;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Providers;

/// <summary>
/// Описывает загруженный Provider без состояния БД, сервера и жизненного цикла persistence.
/// </summary>
/// <remarks>
/// Menu.Core валидирует все технические ключи и клонирует сохраняемые
/// <see cref="JsonElement"/>; сам descriptor не выполняет validation и не бросает.
/// </remarks>
public sealed record MenuProviderDescriptor
{
    /// <summary>Постоянный ASCII-ключ Provider.</summary>
    [JsonPropertyName("providerKey")]
    public string ProviderKey { get; init; } = string.Empty;

    /// <summary>Отображаемое имя Provider.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Версия загруженного плагина.</summary>
    [JsonPropertyName("version")]
    public string PluginVersion { get; init; } = string.Empty;

    /// <summary>Версия Menu API, для которой собран Provider.</summary>
    [JsonPropertyName("menuApiVersion")]
    public int MenuApiVersion { get; init; } = MenuContractVersions.MenuCoreApiVersion;

    /// <summary>Ключи возможностей Provider.</summary>
    [JsonPropertyName("capabilities")]
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

    /// <summary>Несекретные метаданные Provider.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}

/// <summary>Описывает программное меню, экспортируемое Provider.</summary>
public sealed record MenuProviderMenuDescriptor
{
    /// <summary>Постоянный ASCII-ключ меню внутри Provider.</summary>
    [JsonPropertyName("menuKey")]
    public string MenuKey { get; init; } = string.Empty;

    /// <summary>Локализуемое отображаемое имя.</summary>
    [JsonPropertyName("displayName")]
    public LocalizedText DisplayName { get; init; } = new();

    /// <summary>Необязательное локализуемое описание.</summary>
    [JsonPropertyName("description")]
    public LocalizedText? Description { get; init; }

    /// <summary>Делегат открытия, который Menu.Core обязан удалить при unload handle.</summary>
    [JsonIgnore]
    public MenuProviderMenuHandler Handler { get; init; } = null!;

    /// <summary>Несекретные метаданные экспорта.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}

/// <summary>Описывает валидируемое действие, экспортируемое Provider.</summary>
/// <remarks>
/// Menu.Core обязан клонировать сохраняемые <see cref="JsonElement"/>, чтобы их
/// lifetime не зависел от <see cref="JsonDocument"/>, созданного Provider.
/// </remarks>
public sealed record MenuProviderActionDescriptor
{
    /// <summary>Постоянный ASCII-ключ действия внутри Provider.</summary>
    [JsonPropertyName("actionKey")]
    public string ActionKey { get; init; } = string.Empty;

    /// <summary>Локализуемое отображаемое имя.</summary>
    [JsonPropertyName("displayName")]
    public LocalizedText DisplayName { get; init; } = new();

    /// <summary>Необязательное локализуемое описание.</summary>
    [JsonPropertyName("description")]
    public LocalizedText? Description { get; init; }

    /// <summary>Необязательная JSON Schema аргументов для CMS и общей validation.</summary>
    [JsonPropertyName("argumentsSchema")]
    public JsonElement? ArgumentsSchema { get; init; }

    /// <summary>
    /// Обязательный provider-side validator. Menu.Core отклоняет регистрацию,
    /// если делегат отсутствует, даже при наличии <see cref="ArgumentsSchema"/>.
    /// </summary>
    [JsonIgnore]
    public MenuProviderActionValidator Validator { get; init; } = null!;

    /// <summary>Обязательный делегат выполнения после успешной validation.</summary>
    [JsonIgnore]
    public MenuProviderActionHandler Handler { get; init; } = null!;

    /// <summary>Несекретные метаданные экспорта.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}

/// <summary>Передаёт безопасный runtime-контекст Provider handler.</summary>
/// <param name="Caller">Игрок, инициировавший цепочку открытия или действия.</param>
/// <param name="Target">Игрок, для которого выполняется handler.</param>
/// <param name="Arguments">Предварительно проверенные JSON-аргументы.</param>
/// <param name="Depth">Текущая глубина переходов, ограничиваемая Menu.Core.</param>
/// <remarks>Handler не должен сохранять <paramref name="Arguments"/> после завершения вызова без клонирования.</remarks>
public sealed record MenuProviderInvocationContext(
    IPlayer Caller,
    IPlayer Target,
    JsonElement Arguments,
    int Depth
);

/// <summary>Открывает программное меню Provider для одного получателя.</summary>
/// <param name="context">Проверенный runtime-контекст.</param>
/// <returns>Результат открытия без ожидаемых исключений.</returns>
public delegate MenuOperationResult MenuProviderMenuHandler(MenuProviderInvocationContext context);

/// <summary>Выполняет действие Provider для одного получателя.</summary>
/// <param name="context">Проверенный runtime-контекст.</param>
/// <returns>Результат выполнения без ожидаемых исключений.</returns>
public delegate MenuOperationResult MenuProviderActionHandler(MenuProviderInvocationContext context);

/// <summary>Проверяет аргументы действия внутри самого Provider.</summary>
/// <param name="arguments">JSON-аргументы после общей schema validation.</param>
/// <returns>Результат предметной validation Provider.</returns>
public delegate MenuValidationResult MenuProviderActionValidator(JsonElement arguments);
