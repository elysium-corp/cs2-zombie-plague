namespace MapRotation.Core.Domain;

/// <summary>Диагностика применённого каталога; причины исключения — технические коды JSON.</summary>
internal sealed record CatalogMap(long Id, string Key, string MapName, long? WorkshopId,
    bool Enabled, bool EngineValid, bool CurrentMap, bool AllowNomination, bool AllowVote,
    bool AllowAutoRotation, string? PoolExclusion, string? NominationExclusion);
