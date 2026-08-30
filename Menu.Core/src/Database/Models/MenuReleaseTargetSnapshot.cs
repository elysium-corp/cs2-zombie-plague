namespace Menu.Core.Database.Models;

/// <summary>
/// Полностью собранный server-specific artifact активного release.
/// </summary>
internal sealed record MenuReleaseTargetSnapshot(
    long ReleaseId,
    long ReleaseNumber,
    int SchemaVersion,
    int MenuCoreApiVersion,
    string ServerKey,
    string? ServerGroupKey,
    string ArtifactJson,
    string Checksum,
    string CapabilityManifestJson,
    DateTimeOffset PublishedAt);

/// <summary>
/// Текущая production-ссылка сервера с optimistic locking версией.
/// </summary>
internal sealed record MenuReleaseHeadSnapshot(
    string ServerKey,
    long ReleaseId,
    long LockVersion,
    DateTimeOffset UpdatedAt);
