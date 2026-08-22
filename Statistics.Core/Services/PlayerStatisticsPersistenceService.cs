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
            SessionsCount = statistics?.SessionsCount ?? 0,
            PlayTimeSeconds = statistics?.PlayTimeSeconds ?? 0,
            RoundsPlayed = statistics?.RoundsPlayed ?? 0,
            RoundsAsHuman = statistics?.RoundsAsHuman ?? 0,
            RoundsAsZombie = statistics?.RoundsAsZombie ?? 0,
            ZombiesKilled = statistics?.ZombiesKilled ?? 0,
            HeadshotZombieKills = statistics?.HeadshotZombieKills ?? 0,
            InfectionsMade = statistics?.InfectionsMade ?? 0,
            TimesInfected = statistics?.TimesInfected ?? 0,
            DeathsAsHuman = statistics?.DeathsAsHuman ?? 0,
            DeathsAsZombie = statistics?.DeathsAsZombie ?? 0,
            DamageToZombies = statistics?.DamageToZombies ?? 0,
            DamageToHumans = statistics?.DamageToHumans ?? 0,
            SurvivedRounds = statistics?.SurvivedRounds ?? 0,
            HumanWins = statistics?.HumanWins ?? 0,
            ZombieWins = statistics?.ZombieWins ?? 0,
            FirstZombieRounds = statistics?.FirstZombieRounds ?? 0,
            LastHumanRounds = statistics?.LastHumanRounds ?? 0,
            LastHumanSurvivals = statistics?.LastHumanSurvivals ?? 0,
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
        entity.SessionsCount = statistics.SessionsCount;
        entity.PlayTimeSeconds = statistics.PlayTimeSeconds;
        entity.RoundsPlayed = statistics.RoundsPlayed;
        entity.RoundsAsHuman = statistics.RoundsAsHuman;
        entity.RoundsAsZombie = statistics.RoundsAsZombie;
        entity.ZombiesKilled = statistics.ZombiesKilled;
        entity.HeadshotZombieKills = statistics.HeadshotZombieKills;
        entity.InfectionsMade = statistics.InfectionsMade;
        entity.TimesInfected = statistics.TimesInfected;
        entity.DeathsAsHuman = statistics.DeathsAsHuman;
        entity.DeathsAsZombie = statistics.DeathsAsZombie;
        entity.DamageToZombies = statistics.DamageToZombies;
        entity.DamageToHumans = statistics.DamageToHumans;
        entity.SurvivedRounds = statistics.SurvivedRounds;
        entity.HumanWins = statistics.HumanWins;
        entity.ZombieWins = statistics.ZombieWins;
        entity.FirstZombieRounds = statistics.FirstZombieRounds;
        entity.LastHumanRounds = statistics.LastHumanRounds;
        entity.LastHumanSurvivals = statistics.LastHumanSurvivals;
        entity.BestKillStreak = statistics.BestKillStreak;
        entity.BestInfectionStreak = statistics.BestInfectionStreak;
        entity.UpdatedAtUtc = updatedAtUtc;
    }
}

