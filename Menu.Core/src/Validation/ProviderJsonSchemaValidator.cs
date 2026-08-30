using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Menu.Api.Enums;
using Menu.Api.Results;

namespace Menu.Core.Validation;

/// <summary>
/// Проверяет переносимое подмножество JSON Schema, общее для Menu.Core и Flute.
/// </summary>
internal static class ProviderJsonSchemaValidator
{
    private const int MaximumDepth = 8;
    private const int MaximumIssues = 64;
    private const int MaximumArrayItems = 256;
    private static readonly JsonElement EmptyObject = JsonSerializer.SerializeToElement(new { });

    internal static MenuValidationResult Validate(JsonElement? arguments, JsonElement schema)
    {
        var issues = new List<MenuValidationIssue>();
        ValidateValue(arguments ?? EmptyObject, schema, "$", issues, 0);
        return new MenuValidationResult
        {
            IsValid = issues.All(static issue => issue.Severity != MenuValidationSeverity.Error),
            Issues = issues,
        };
    }

    private static void ValidateValue(
        JsonElement value,
        JsonElement schema,
        string path,
        ICollection<MenuValidationIssue> issues,
        int depth)
    {
        if (issues.Count >= MaximumIssues)
        {
            return;
        }

        if (depth > MaximumDepth)
        {
            Add(issues, "provider.schema_depth", "Provider argument schema is too deeply nested.", path);
            return;
        }

        if (schema.ValueKind != JsonValueKind.Object)
        {
            Add(issues, "provider.schema_invalid", "Provider argument schema must be an object.", path);
            return;
        }

        if (schema.TryGetProperty("enum", out var enumeration))
        {
            if (enumeration.ValueKind != JsonValueKind.Array)
            {
                Add(issues, "provider.schema_enum", "Provider schema enum must be an array.", path);
                return;
            }

            if (!enumeration.EnumerateArray().Any(candidate => JsonElement.DeepEquals(candidate, value)))
            {
                Add(issues, "provider.argument_enum", "Provider argument is not one of the allowed values.", path);
                return;
            }
        }

        if (!schema.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            if (!schema.EnumerateObject().Any())
            {
                return;
            }

            Add(issues, "provider.schema_type", "Provider argument schema must declare a type.", path);
            return;
        }

        var type = typeElement.GetString()!;
        if (!HasType(value, type))
        {
            Add(issues, "provider.argument_type", $"Provider argument must be {type}.", path);
            return;
        }

        switch (type)
        {
            case "string":
                ValidateString(value.GetString()!, schema, path, issues);
                break;
            case "integer":
            case "number":
                ValidateNumber(value, schema, path, issues);
                break;
            case "array":
                ValidateArray(value, schema, path, issues, depth);
                break;
            case "object":
                ValidateObject(value, schema, path, issues, depth);
                break;
        }
    }

    private static bool HasType(JsonElement value, string type) => type switch
    {
        "string" => value.ValueKind == JsonValueKind.String,
        "integer" => value.ValueKind == JsonValueKind.Number
                     && (value.TryGetInt64(out _) || value.TryGetUInt64(out _)),
        "number" => value.ValueKind == JsonValueKind.Number,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "null" => value.ValueKind == JsonValueKind.Null,
        _ => false,
    };

    private static void ValidateString(
        string value,
        JsonElement schema,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        var length = value.EnumerateRunes().Count();
        if (TryReadNonNegativeInteger(schema, "minLength", out var minimumLength)
            && length < minimumLength)
        {
            Add(issues, "provider.argument_min_length", "Provider argument is shorter than allowed.", path);
        }

        if (TryReadNonNegativeInteger(schema, "maxLength", out var maximumLength)
            && length > maximumLength)
        {
            Add(issues, "provider.argument_max_length", "Provider argument is longer than allowed.", path);
        }

        if (!schema.TryGetProperty("pattern", out var patternElement))
        {
            return;
        }

        if (patternElement.ValueKind != JsonValueKind.String)
        {
            Add(issues, "provider.schema_pattern", "Provider schema pattern must be a string.", path);
            return;
        }

        try
        {
            if (!Regex.IsMatch(
                    value,
                    patternElement.GetString()!,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(50)))
            {
                Add(issues, "provider.argument_pattern", "Provider argument does not match the required pattern.", path);
            }
        }
        catch (ArgumentException)
        {
            Add(issues, "provider.schema_pattern", "Provider schema contains an invalid pattern.", path);
        }
        catch (RegexMatchTimeoutException)
        {
            Add(issues, "provider.schema_pattern_timeout", "Provider schema pattern exceeded the validation timeout.", path);
        }
    }

    private static void ValidateNumber(
        JsonElement value,
        JsonElement schema,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (!TryReadNumber(value, out var number))
        {
            Add(issues, "provider.argument_number", "Provider argument number cannot be represented safely.", path);
            return;
        }

        if (schema.TryGetProperty("minimum", out var minimumElement))
        {
            if (!TryReadNumber(minimumElement, out var minimum))
            {
                Add(issues, "provider.schema_minimum", "Provider schema minimum must be a number.", path);
            }
            else if (number < minimum)
            {
                Add(issues, "provider.argument_minimum", "Provider argument is below the minimum.", path);
            }
        }

        if (schema.TryGetProperty("maximum", out var maximumElement))
        {
            if (!TryReadNumber(maximumElement, out var maximum))
            {
                Add(issues, "provider.schema_maximum", "Provider schema maximum must be a number.", path);
            }
            else if (number > maximum)
            {
                Add(issues, "provider.argument_maximum", "Provider argument is above the maximum.", path);
            }
        }
    }

    private static void ValidateArray(
        JsonElement value,
        JsonElement schema,
        string path,
        ICollection<MenuValidationIssue> issues,
        int depth)
    {
        var items = value.EnumerateArray().ToArray();
        var maximumItems = MaximumArrayItems;
        if (schema.TryGetProperty("maxItems", out var maximumElement))
        {
            if (!maximumElement.TryGetInt32(out maximumItems) || maximumItems < 0)
            {
                Add(issues, "provider.schema_max_items", "Provider schema maxItems must be non-negative.", path);
                return;
            }

            maximumItems = Math.Min(maximumItems, MaximumArrayItems);
        }

        if (items.Length > maximumItems)
        {
            Add(issues, "provider.argument_max_items", "Provider argument list is too large.", path);
            return;
        }

        if (!schema.TryGetProperty("items", out var itemSchema))
        {
            return;
        }

        if (itemSchema.ValueKind != JsonValueKind.Object)
        {
            Add(issues, "provider.schema_items", "Provider schema items must be an object.", path);
            return;
        }

        for (var index = 0; index < items.Length && issues.Count < MaximumIssues; index++)
        {
            ValidateValue(items[index], itemSchema, $"{path}.{index}", issues, depth + 1);
        }
    }

    private static void ValidateObject(
        JsonElement value,
        JsonElement schema,
        string path,
        ICollection<MenuValidationIssue> issues,
        int depth)
    {
        JsonElement properties = default;
        if (schema.TryGetProperty("properties", out properties)
            && properties.ValueKind != JsonValueKind.Object)
        {
            Add(issues, "provider.schema_properties", "Provider schema properties must be an object.", path);
            return;
        }

        if (schema.TryGetProperty("required", out var required))
        {
            if (required.ValueKind != JsonValueKind.Array)
            {
                Add(issues, "provider.schema_required", "Provider schema required must be an array.", path);
                return;
            }

            foreach (var requiredName in required.EnumerateArray())
            {
                if (requiredName.ValueKind != JsonValueKind.String)
                {
                    Add(issues, "provider.schema_required", "Provider schema required keys must be strings.", path);
                    continue;
                }

                var name = requiredName.GetString()!;
                if (!value.TryGetProperty(name, out _))
                {
                    Add(issues, "provider.argument_required", "Required provider argument is missing.", $"{path}.{name}");
                }
            }
        }

        var additionalAllowed = schema.TryGetProperty("additionalProperties", out var additional)
                                && additional.ValueKind == JsonValueKind.True;
        foreach (var property in value.EnumerateObject())
        {
            if (properties.ValueKind != JsonValueKind.Object
                || !properties.TryGetProperty(property.Name, out var propertySchema))
            {
                if (!additionalAllowed)
                {
                    Add(issues, "provider.argument_unknown", "Unknown provider argument.", $"{path}.{property.Name}");
                }

                continue;
            }

            ValidateValue(property.Value, propertySchema, $"{path}.{property.Name}", issues, depth + 1);
            if (issues.Count >= MaximumIssues)
            {
                return;
            }
        }
    }

    private static bool TryReadNonNegativeInteger(JsonElement schema, string name, out int value)
    {
        value = 0;
        return schema.TryGetProperty(name, out var element)
               && element.TryGetInt32(out value)
               && value >= 0;
    }

    private static bool TryReadNumber(JsonElement value, out decimal number)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out number))
        {
            return true;
        }

        number = default;
        return false;
    }

    private static void Add(
        ICollection<MenuValidationIssue> issues,
        string code,
        string message,
        string path)
    {
        if (issues.Count >= MaximumIssues)
        {
            return;
        }

        issues.Add(new MenuValidationIssue
        {
            Severity = MenuValidationSeverity.Error,
            Code = code,
            Message = message,
            Path = path,
        });
    }
}
