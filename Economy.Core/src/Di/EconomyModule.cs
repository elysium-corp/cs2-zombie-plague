using Common.Database;
using Common.Database.Storages;
using Common.Database.Utils;
using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Repository;
using Economy.Core.Data.Store;
using Economy.Core.Database;
using Economy.Core.Database.Entities;
using Economy.Core.Initializer;
using Economy.Core.Services;
using SwiftlyS2.Shared;

namespace Economy.Core.Di;

internal sealed class EconomyModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        BuildConfigs(service);
        
        service.AddSwiftly(core);
        
        BuildSingletons(service);
        AddDatabase(service);
        
        return (service.BuildServiceProvider(), service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<EconomyConfig>(
            service: service,
            name: "economy.json",
            section: "EconomyConfig"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<IAccountPersistenceService, AccountPersistenceService>(service);
        AddSingleton<IEconomyService, EconomyService>(service);
        AddSingleton<PlayerSessionStore<PlayerAccountState>>(service);
        AddSingleton<PlayerAccountService>(service);
    }
    
    private void AddDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = EconomyDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };

        service.AddPostgreSqlDatabase<EconomyDbContext>(core, options);

        service.AddSteamEntityStore<EconomyDbContext, AccountEntity>();
    }
}