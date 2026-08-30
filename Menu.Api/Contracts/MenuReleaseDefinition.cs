using System.Text.Json;
using System.Text.Json.Serialization;

namespace Menu.Api.Contracts;

/// <summary>
/// Представляет полностью нормализованный атомарный Release для runtime, preview,
/// Last Known Good и fallback.
/// </summary>
public sealed record MenuReleaseDefinition
{
    /// <summary>Версия JSON-схемы payload.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = MenuContractVersions.SchemaVersion;

    /// <summary>Минимальная версия публичного Menu API.</summary>
    [JsonPropertyName("menuCoreApiVersion")]
    public int MenuCoreApiVersion { get; init; } = MenuContractVersions.MenuCoreApiVersion;

    /// <summary>Неизменяемый идентификатор Release.</summary>
    [JsonPropertyName("releaseId")]
    public long ReleaseId { get; init; }

    /// <summary>Момент генерации transport payload.</summary>
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>SHA-256 canonical payload либо <c>null</c> до формирования артефакта.</summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; init; }

    /// <summary>Опубликованные revision меню, входящие в Release.</summary>
    [JsonPropertyName("menus")]
    public IReadOnlyList<MenuDefinition> Menus { get; init; } = Array.Empty<MenuDefinition>();

    /// <summary>Командные aliases, входящие в Release.</summary>
    [JsonPropertyName("commands")]
    public IReadOnlyList<MenuCommandDefinition> Commands { get; init; }
        = Array.Empty<MenuCommandDefinition>();

    /// <summary>Несекретные расширяемые метаданные Release.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}
