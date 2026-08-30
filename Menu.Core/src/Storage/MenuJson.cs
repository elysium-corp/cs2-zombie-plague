using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Menu.Api.Contracts;

namespace Menu.Core.Storage;

internal static class MenuJson
{
    public const int CurrentSchemaVersion = MenuContractVersions.SchemaVersion;
    public const int CurrentMenuCoreApiVersion = MenuContractVersions.MenuCoreApiVersion;

    public static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public static string ComputeChecksum<T>(T value)
    {
        var element = JsonSerializer.SerializeToElement(value, SerializerOptions);
        var canonical = Canonicalize(element, omitRootChecksum: true);
        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }

    public static bool FixedTimeChecksumEquals(string? expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || expected.Length != actual.Length)
        {
            return false;
        }

        var expectedBytes = System.Text.Encoding.ASCII.GetBytes(expected.ToLowerInvariant());
        var actualBytes = System.Text.Encoding.ASCII.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public static byte[] Canonicalize(JsonElement element, bool omitRootChecksum = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
               {
                   Indented = false,
                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                   SkipValidation = false
               }))
        {
            WriteCanonical(writer, element, omitRootChecksum, isRoot: true);
        }

        return buffer.WrittenSpan.ToArray();
    }

    public static MenuReleaseDefinition? DeserializeRelease(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json, DocumentOptions);
        EnsureNoDuplicateProperties(document.RootElement, "$");
        return document.RootElement.Deserialize<MenuReleaseDefinition>(SerializerOptions);
    }

    public static MenuReleaseDefinition? DeserializeRelease(ReadOnlyMemory<byte> json)
    {
        using var document = JsonDocument.Parse(json, DocumentOptions);
        EnsureNoDuplicateProperties(document.RootElement, "$");
        return document.RootElement.Deserialize<MenuReleaseDefinition>(SerializerOptions);
    }

    private static JsonDocumentOptions DocumentOptions => new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = null,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            MaxDepth = 64,
            NumberHandling = JsonNumberHandling.Strict,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = false,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
            }
        };
    }

    private static void WriteCanonical(
        Utf8JsonWriter writer,
        JsonElement element,
        bool omitRootChecksum,
        bool isRoot)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                // Все schema keys и проверяемые arbitrary keys ограничены ASCII,
                // поэтому Ordinal даёт тот же порядок, что byte-wise UTF-8 sort в PHP.
                foreach (var property in element.EnumerateObject().OrderBy(static p => p.Name, StringComparer.Ordinal))
                {
                    if (isRoot && omitRootChecksum &&
                        string.Equals(property.Name, "checksum", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value, omitRootChecksum: false, isRoot: false);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item, omitRootChecksum: false, isRoot: false);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                WriteCanonicalNumber(writer, element);
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new JsonException($"Unsupported JSON token: {element.ValueKind}.");
        }
    }

    private static void WriteCanonicalNumber(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.TryGetInt64(out var signed))
        {
            writer.WriteNumberValue(signed);
            return;
        }

        if (element.TryGetUInt64(out var unsigned))
        {
            writer.WriteNumberValue(unsigned);
            return;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            writer.WriteRawValue(decimalValue.ToString("G29", CultureInfo.InvariantCulture));
            return;
        }

        if (element.TryGetDouble(out var doubleValue) && double.IsFinite(doubleValue))
        {
            writer.WriteNumberValue(doubleValue);
            return;
        }

        throw new JsonException("JSON number cannot be represented canonically.");
    }

    private static void EnsureNoDuplicateProperties(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException($"Duplicate JSON property at {path}.");
                }

                EnsureNoDuplicateProperties(property.Value, $"{path}.{property.Name}");
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                EnsureNoDuplicateProperties(item, $"{path}[{index++}]");
            }
        }
    }
}
