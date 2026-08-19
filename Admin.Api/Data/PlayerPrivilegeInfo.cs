namespace Admin.Api.Data;

public sealed record PlayerPrivilegeInfo(
    string Key,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public bool IsPermanent => ExpiresAtUtc == null;
}