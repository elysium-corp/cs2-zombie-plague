using System.Text.Json;
using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Contracts;

/// <summary>Представляет одну опубликованную revision нормализованного меню.</summary>
public sealed record MenuDefinition
{
    /// <summary>Стабильный ASCII-ключ меню.</summary>
    [JsonPropertyName("menuKey")]
    public string MenuKey { get; init; } = string.Empty;

    /// <summary>Неизменяемый номер revision меню.</summary>
    [JsonPropertyName("revision")]
    public int Revision { get; init; }

    /// <summary>Жизненный цикл revision; runtime принимает только Published.</summary>
    [JsonPropertyName("status")]
    public MenuLifecycleStatus Status { get; init; } = MenuLifecycleStatus.Draft;

    /// <summary>Необязательный Provider-владелец меню.</summary>
    [JsonPropertyName("providerKey")]
    public string? ProviderKey { get; init; }

    /// <summary>Локализуемый заголовок.</summary>
    [JsonPropertyName("title")]
    public LocalizedText Title { get; init; } = new();

    /// <summary>Необязательное локализуемое описание для CMS.</summary>
    [JsonPropertyName("description")]
    public LocalizedText? Description { get; init; }

    /// <summary>Область применения меню.</summary>
    [JsonPropertyName("scope")]
    public MenuScopeDefinition Scope { get; init; } = new();

    /// <summary>Необязательное родительское меню для Back navigation.</summary>
    [JsonPropertyName("parent")]
    public MenuReferenceDefinition? Parent { get; init; }

    /// <summary>Политика доступа к меню.</summary>
    [JsonPropertyName("access")]
    public MenuAccessPolicyDefinition Access { get; init; } = new();

    /// <summary>Получатели при открытии без runtime-переопределения.</summary>
    [JsonPropertyName("audience")]
    public MenuAudienceDefinition Audience { get; init; } = new();

    /// <summary>Нормализованные настройки Swiftly Menu adapter.</summary>
    [JsonPropertyName("design")]
    public MenuDesignDefinition Design { get; init; } = new();

    /// <summary>Упорядоченные пункты меню.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MenuItemDefinition> Items { get; init; } = Array.Empty<MenuItemDefinition>();

    /// <summary>Ключи capabilities, обязательные для целевого сервера.</summary>
    [JsonPropertyName("requiredFeatures")]
    public IReadOnlyList<string> RequiredFeatures { get; init; } = Array.Empty<string>();

    /// <summary>Несекретные расширяемые метаданные меню.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}
