namespace Localization.Api;

/// <summary>
/// Приводит технические ключи локализации к единому точечному формату.
/// </summary>
public static class LocalizationKey
{
    private static readonly char[] Separators = ['.', '_', '-', ':', ' ', '\t', '\r', '\n'];

    /// <summary>
    /// Разделяет слова точками и делает первую букву каждого сегмента заглавной.
    /// </summary>
    /// <param name="value">Исходный ключ или внутренний идентификатор.</param>
    /// <returns>Канонический ключ.</returns>
    /// <exception cref="ArgumentException">Исходное значение пусто.</exception>
    public static string Canonicalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var segments = value.Trim()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((segment, index) =>
                index == 0 && string.Equals(segment, "Tags", StringComparison.OrdinalIgnoreCase)
                    ? "Tag"
                    : $"{char.ToUpperInvariant(segment[0])}{segment[1..]}")
            .ToArray();

        return segments.Length == 0
            ? throw new ArgumentException("Ключ не содержит допустимых сегментов.", nameof(value))
            : string.Join('.', segments);
    }
}
