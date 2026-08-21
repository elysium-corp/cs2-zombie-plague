namespace Admin.Core.Data;

/// <summary>
/// Представляет активную блокировку игрока.
/// </summary>
/// <param name="Reason">
/// Причина блокировки.
/// </param>
/// <param name="ExpiresAtUtc">
/// Дата окончания блокировки в UTC.
/// <c>null</c> означает бессрочную блокировку.
/// </param>
internal sealed record ActiveBan(
    string Reason,
    DateTime? ExpiresAtUtc
);