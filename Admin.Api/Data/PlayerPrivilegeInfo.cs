namespace Admin.Api.Data;

/// <summary>
/// Содержит информацию о сохранённом назначении привилегии конкретному игроку.
/// </summary>
/// <param name="Key">
/// Ключ назначенной привилегии в формате <c>group.id</c>.
/// </param>
/// <param name="ExpiresAtUtc">
/// Время окончания действия привилегии в UTC.
/// Значение <c>null</c> означает бессрочное назначение.
/// </param>
/// <param name="CreatedAtUtc">
/// Время создания текущего назначения в UTC.
/// </param>
/// <param name="UpdatedAtUtc">
/// Время последнего изменения текущего назначения в UTC.
/// </param>
/// <remarks>
/// В отличие от <see cref="IPrivilege"/>, данный тип описывает не определение
/// привилегии, а её сохранённое назначение определённому игроку.
/// </remarks>
public sealed record PlayerPrivilegeInfo(
    string Key,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    /// <summary>
    /// Возвращает <c>true</c>, если назначение является бессрочным.
    /// </summary>
    public bool IsExpired => ExpiresAtUtc is { } expiresAt && expiresAt <= DateTime.UtcNow;
}