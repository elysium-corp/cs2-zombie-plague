using System.Text.Json;
using System.Text.Json.Nodes;

namespace Menu.Core.Providers;

/// <summary>
/// Собирает окончательный JSON-объект аргументов одинаково для Publish validation
/// и runtime-вызова Provider Action.
/// </summary>
internal static class MenuActionArguments
{
    private static readonly JsonElement EmptyObject = JsonSerializer.SerializeToElement(new { });

    internal static JsonElement Compose(JsonElement? configured, JsonElement? changedValue)
    {
        if (changedValue is null)
        {
            return configured?.Clone() ?? EmptyObject;
        }

        var root = configured is { ValueKind: JsonValueKind.Object } value
            ? JsonNode.Parse(value.GetRawText())?.AsObject() ?? new JsonObject()
            : new JsonObject();
        root["value"] = JsonNode.Parse(changedValue.Value.GetRawText());
        return JsonSerializer.SerializeToElement(root);
    }
}
