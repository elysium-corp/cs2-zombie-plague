namespace Menu.Core.Database.Models;

/// <summary>Параметры новой runtime-сессии Menu.Core на игровом сервере.</summary>
internal sealed record MenuRuntimeStatusRegistration(
    string ServerKey,
    Guid RuntimeSessionId,
    string MenuCoreVersion,
    string SwiftlyVersion,
    int MenuApiVersion,
    int SchemaVersion,
    string CapabilitiesJson);

/// <summary>Session-aware lease для heartbeat и status updates.</summary>
internal sealed record MenuRuntimeStatusLease(
    string ServerKey,
    Guid RuntimeSessionId,
    long Generation);

/// <summary>Полный наблюдаемый статус текущего runtime snapshot.</summary>
internal sealed record MenuRuntimeStatusUpdate(
    long? ActiveReleaseId,
    string? ActiveChecksum,
    string? LoadedSource,
    DateTimeOffset? LastDbSyncAt,
    long? LastKnownGoodReleaseId,
    long? FallbackReleaseId,
    string ValidationStatus,
    string? LastError);
