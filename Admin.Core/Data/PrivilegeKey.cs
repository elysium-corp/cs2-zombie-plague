namespace Admin.Core.Data;

/// <summary>
/// Представляет разобранный логический ключ привилегии.
/// </summary>
/// <param name="Group">Группа привилегии.</param>
/// <param name="Code">Идентификатор привилегии внутри группы.</param>
internal readonly record struct PrivilegeKey(string Group, string Code)
{
    /// <summary>
    /// Пытается разобрать строковый ключ привилегии в формате <c>group.code</c>.
    /// </summary>
    public static bool TryParse(string value, out PrivilegeKey key)
    {
        key = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separatorIndex = value.IndexOf('.');

        if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
        {
            return false;
        }

        if (value.IndexOf('.', separatorIndex + 1) >= 0)
        {
            return false;
        }

        key = new PrivilegeKey(
            value[..separatorIndex],
            value[(separatorIndex + 1)..]
        );

        return true;
    }

    public override string ToString()
    {
        return $"{Group}.{Code}";
    }
}