using Admin.Api.Data;

namespace Admin.Core.Data;

/// <summary>
/// Внутренняя неизменяемая реализация зарегистрированной привилегии.
/// </summary>
internal sealed class Privilege : IPrivilege
{
    /// <inheritdoc />
    public required string Id { get; init; }

    /// <inheritdoc />
    public required string Group { get; init; }

    /// <inheritdoc />
    public required IReadOnlySet<string> Permissions { get; init; }

    /// <inheritdoc />
    /// <inheritdoc />
    public string DisplayName { get; init; } = string.Empty;

    /// <inheritdoc />
    public string ChatColor { get; init; } = "default";

    /// <inheritdoc />
    public string HudColor { get; init; } = "#ffffff";

    /// <inheritdoc />
    public int ColorPriority { get; init; }

    /// <inheritdoc />
    public string Key => $"{Group}.{Id}";
}