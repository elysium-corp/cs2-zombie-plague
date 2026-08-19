namespace Admin.Core.Data;

/// <summary>
/// Представляет сохранённое назначение привилегии игроку внутри Admin.Core.
/// </summary>
/// <param name="Key">Ключ привилегии.</param>
/// <param name="ExpiresAtUtc">
/// Время окончания действия в UTC или <c>null</c> для бессрочного назначения.
/// </param>
/// <param name="CreatedAtUtc">Время создания текущего назначения в UTC.</param>
/// <param name="UpdatedAtUtc">Время последнего изменения назначения в UTC.</param>
internal sealed record PlayerPrivilege(
    string Key,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);