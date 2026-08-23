using Microsoft.EntityFrameworkCore;
using Statistics.Core.Data;
using Statistics.Core.Database;
using Statistics.Core.Database.Entities;

namespace Statistics.Core.Services;

internal sealed class PlayerStatisticsPersistenceService(
    IDbContextFactory<StatisticsDbContext> dbContextFactory
) : IPlayerStatisticsPersistenceService
{
    public async Task<PlayerStatisticsSnapshot?> LoadAsync(
        ulong steamId,
        CancellationToken cancellationToken = default
    )
    {
        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var player = await context.Players
            .AsNoTracking()
            .Include(x => x.Statistics)
            .SingleOrDefaultAsync(x => x.SteamId == databaseSteamId, cancellationToken)
            .ConfigureAwait(false);

        return player is null ? null : CreateSnapshot(player);
    }

    public async Task SaveAsync(
        ulong steamId,
        PlayerStatisticsSnapshot statistics,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var player = await context.Players
            .Include(x => x.Statistics)
            .SingleOrDefaultAsync(x => x.SteamId == databaseSteamId, cancellationToken)
            .ConfigureAwait(false);

        if (player is null)
        {
            player = new PlayerEntity
            {
                SteamId = databaseSteamId,
                LastKnownName = statistics.LastKnownName,
                FirstSeenAtUtc = statistics.FirstSeenAtUtc,
                LastSeenAtUtc = statistics.LastSeenAtUtc,
                Statistics = new PlayerStatisticsEntity
                {
                    SteamId = databaseSteamId
                }
            };

            context.Players.Add(player);
        }
        else
        {
            player.LastKnownName = statistics.LastKnownName;
            player.LastSeenAtUtc = statistics.LastSeenAtUtc;

            if (player.Statistics is null)
            {
                player.Statistics = new PlayerStatisticsEntity
                {
                    SteamId = databaseSteamId,
                    Player = player
                };

                context.PlayerStatistics.Add(player.Statistics);
            }
        }

        ApplyStatistics(player.Statistics!, statistics, now);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static PlayerStatisticsSnapshot CreateSnapshot(PlayerEntity player)
    {
        var statistics = player.Statistics;

        return new PlayerStatisticsSnapshot
        {
            LastKnownName = player.LastKnownName,
            FirstSeenAtUtc = player.FirstSeenAtUtc,
            LastSeenAtUtc = player.LastSeenAtUtc,
            Points = statistics?.Points ?? 0,
            PlayTimeSeconds = statistics?.PlayTimeSeconds ?? 0,
            ZombiesKilled = statistics?.ZombiesKilled ?? 0,
            InfectionsMade = statistics?.InfectionsMade ?? 0,
            TimesInfected = statistics?.TimesInfected ?? 0,
            Deaths = statistics?.Deaths ?? 0,
            HumanWins = statistics?.HumanWins ?? 0,
            ZombieWins = statistics?.ZombieWins ?? 0,
            BestKillStreak = statistics?.BestKillStreak ?? 0,
            BestInfectionStreak = statistics?.BestInfectionStreak ?? 0
        };
    }

    private static void ApplyStatistics(
        PlayerStatisticsEntity entity,
        PlayerStatisticsSnapshot statistics,
        DateTime updatedAtUtc
    )
    {
        entity.Points = statistics.Points;
        entity.PlayTimeSeconds = statistics.PlayTimeSeconds;
        entity.ZombiesKilled = statistics.ZombiesKilled;
        entity.InfectionsMade = statistics.InfectionsMade;
        entity.TimesInfected = statistics.TimesInfected;
        entity.Deaths = statistics.Deaths;
        entity.HumanWins = statistics.HumanWins;
        entity.ZombieWins = statistics.ZombieWins;
        entity.BestKillStreak = statistics.BestKillStreak;
        entity.BestInfectionStreak = statistics.BestInfectionStreak;
        entity.UpdatedAtUtc = updatedAtUtc;
    }
}
