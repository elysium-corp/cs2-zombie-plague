using Common.Di;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Repository;
using Economy.Core.Database;
using Economy.Core.Initializer;
using Economy.Core.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        
        service.AddDbContextFactory<EconomyDbContext>(options =>
        {
            var connectionProvider = new DatabaseConnectionProvider(core);
            var connectionString = connectionProvider.GetPostgreSqlConnectionString("elysium_zp_server_1");
            options.UseNpgsql(
                connectionString,   
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(10);
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "economy");
                })
                .ConfigureWarnings(warnings =>
                {
                    warnings.Ignore(RelationalEventId.CommandExecuted);
                });
        });
        
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
        AddSingleton<EconomyDatabaseInitializer>(service);
        AddSingleton<IAccountPersistenceService, AccountPersistenceService>(service);
        AddSingleton<IAccountRepository, AccountRepository>(service);
        AddSingleton<IEconomyService, EconomyService>(service);
    }
}