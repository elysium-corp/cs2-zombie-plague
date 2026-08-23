using Common.Database.Migrator;
using Common.Di;
using Microsoft.Extensions.Logging;
using Statistics.Core.Database;
using Statistics.Core.Di;
using Statistics.Core.Services;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace Statistics.Core;

[PluginMetadata(
    Id = "Statistics.Core",
    Version = "0.1.0",
    Name = "Statistics Core",
    Author = "illusion & fdrinv",
    Description = "Collects player statistics"
)]
internal sealed partial class Statistics(ISwiftlyCore core) : Plugin<StatisticsModule>(core)
{
    private readonly Lazy<DatabaseMigrator<StatisticsDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<StatisticsDbContext>>();

    private readonly Lazy<PlayerStatisticsService> _playerStatisticsService =
        GetRequiredServiceLazy<PlayerStatisticsService>();

    private readonly Lazy<StatisticsCollector> _statisticsCollector =
        GetRequiredServiceLazy<StatisticsCollector>();

    protected override void OnStart()
    {
        TryMigrateDatabase();
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(
            IZombiePlagueApi.SharedApiKey
        );

        _statisticsCollector.Value.Initialize(zombiePlagueApi);
    }

    protected override void OnReady()
    {
        _playerStatisticsService.Value.InitializeExistingPlayers();
        _statisticsCollector.Value.Start();
        _playerStatisticsService.Value.Start();
    }

    protected override void OnUnload()
    {
        _statisticsCollector.Value.Stop();
        _playerStatisticsService.Value.StopAndWait();
    }

    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Statistics database migration failed. Temporary statistics will be used."
            );
        }
    }
}

