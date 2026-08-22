using Statistics.Core.Data;

namespace Statistics.Core.Services;

internal interface IPlayerStatisticsPersistenceService
{
    Task<PlayerStatisticsSnapshot?> LoadAsync(
        ulong steamId,
        CancellationToken cancellationToken = default
    );

    Task SaveAsync(
        ulong steamId,
        PlayerStatisticsSnapshot statistics,
        CancellationToken cancellationToken = default
    );
}

