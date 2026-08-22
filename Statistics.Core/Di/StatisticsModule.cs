using Common.Database;
using Common.Database.Storages;
using Common.Database.Utils;
using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using Statistics.Core.Data;
using Statistics.Core.Database;
using Statistics.Core.Services;
using SwiftlyS2.Shared;

namespace Statistics.Core.Di;

internal sealed class StatisticsModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(Core);

        BuildSingletons(service);
        AddDatabase(service);

        return (service.BuildServiceProvider(), service);
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<PlayerSessionStore<PlayerStatisticsState>>(service);
        AddSingleton<IPlayerStatisticsPersistenceService, PlayerStatisticsPersistenceService>(service);
        AddSingleton<PlayerStatisticsService>(service);
        AddSingleton<StatisticsCollector>(service);
    }

    private void AddDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = StatisticsDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };

        service.AddPostgreSqlDatabase<StatisticsDbContext>(Core, options);
    }
}

