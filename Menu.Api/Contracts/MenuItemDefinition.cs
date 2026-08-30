using System.Text.Json;
using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Contracts;

/// <summary>Представляет один нормализованный пункт меню.</summary>
public sealed record MenuItemDefinition
{
    /// <summary>Стабильный ASCII-ключ пункта внутри меню.</summary>
    [JsonPropertyName("itemKey")]
    public string ItemKey { get; init; } = string.Empty;

    /// <summary>Тип пункта Swiftly Menu adapter.</summary>
    [JsonPropertyName("kind")]
    public MenuItemKind Kind { get; init; } = MenuItemKind.Text;

    /// <summary>Основной локализуемый текст.</summary>
    [JsonPropertyName("text")]
    public LocalizedText Text { get; init; } = new();

    /// <summary>Необязательный комментарий.</summary>
    [JsonPropertyName("comment")]
    public LocalizedText? Comment { get; init; }

    /// <summary>Явно отключает пункт независимо от ACL.</summary>
    [JsonPropertyName("disabled")]
    public bool Disabled { get; init; }

    /// <summary>Безопасный именованный стиль, поддерживаемый adapter.</summary>
    [JsonPropertyName("style")]
    public string? Style { get; init; }

    /// <summary>Политика доступа; по умолчанию наследуется от меню.</summary>
    [JsonPropertyName("access")]
    public MenuAccessPolicyDefinition Access { get; init; } = new()
    {
        Kind = MenuAccessPolicyKind.Inherited
    };

    /// <summary>Представление пункта при отсутствии permission.</summary>
    [JsonPropertyName("noAccessBehavior")]
    public MenuNoAccessBehavior NoAccessBehavior { get; init; } = MenuNoAccessBehavior.Hide;

    /// <summary>Представление пункта при выгруженном Provider.</summary>
    [JsonPropertyName("providerUnavailableBehavior")]
    public ProviderUnavailableBehavior ProviderUnavailableBehavior { get; init; }
        = ProviderUnavailableBehavior.Disable;

    /// <summary>Типизированные параметры начального значения.</summary>
    [JsonPropertyName("value")]
    public MenuItemValueDefinition Value { get; init; } = new();

    /// <summary>Действие при выборе пункта.</summary>
    [JsonPropertyName("action")]
    public MenuActionDefinition? Action { get; init; }

    /// <summary>Действие после изменения Checkbox, Choice или Slider.</summary>
    [JsonPropertyName("onChange")]
    public MenuActionDefinition? OnChange { get; init; }

    /// <summary>Несекретные расширяемые метаданные пункта.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
        = new Dictionary<string, JsonElement>();
}

/// <summary>Описывает значение и ограничения интерактивного пункта.</summary>
public sealed record MenuItemValueDefinition
{
    /// <summary>Начальное JSON-значение пункта.</summary>
    [JsonPropertyName("initial")]
    public JsonElement? Initial { get; init; }

    /// <summary>Варианты пункта Choice.</summary>
    [JsonPropertyName("choices")]
    public IReadOnlyList<MenuChoiceOptionDefinition> Choices { get; init; }
        = Array.Empty<MenuChoiceOptionDefinition>();

    /// <summary>Минимум Slider.</summary>
    [JsonPropertyName("minimum")]
    public decimal? Minimum { get; init; }

    /// <summary>Максимум Slider.</summary>
    [JsonPropertyName("maximum")]
    public decimal? Maximum { get; init; }

    /// <summary>Шаг Slider.</summary>
    [JsonPropertyName("step")]
    public decimal? Step { get; init; }
}

/// <summary>Описывает вариант выбора пункта Choice.</summary>
public sealed record MenuChoiceOptionDefinition
{
    /// <summary>Стабильный ASCII-ключ варианта.</summary>
    [JsonPropertyName("optionKey")]
    public string OptionKey { get; init; } = string.Empty;

    /// <summary>Локализуемая подпись варианта.</summary>
    [JsonPropertyName("text")]
    public LocalizedText Text { get; init; } = new();

    /// <summary>JSON-значение, передаваемое OnChange.</summary>
    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }
}
