using Admin.Api;
using Common.Database;
using Common.Database.Storages;
using Common.Database.Utils;
using Common.Di;
using Common.Di.Utils;
using CustomKnife.Data.Configs;
using CustomKnife.Data.Menus;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Store;
using CustomKnife.Database;
using CustomKnife.Database.Entities;
using CustomKnife.Initializer;
using CustomKnife.Services;
using Menu.Api.Extensions;
using Localization.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace CustomKnife.Di;

internal sealed class CustomKnifeModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        BuildDatabase(service);
        
        BuildConfigs(service);
        BuildKnifeConfigs(service);
        BuildSingletons(service);
        BuildKnives(service);

        return (service.BuildServiceProvider(), service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<KnifeConfig>(
            service: service,
            name: "knives.json",
            section: "KnifeConfig"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        service.AddSharedInterface<IZombiePlagueApi>();
        service.AddSharedInterface<ILocalizationApi>();

        AddSingleton<AdminApiProxy>(service);
        AddSingleton<IAdminApi>(service, provider => provider.GetRequiredService<AdminApiProxy>());
        AddSingleton<IKnifeAuthorizationService, KnifeAuthorizationService>(service);
        AddSingleton<KnifeAccessMonitor>(service);
        
        AddSingleton<KnifeMenu>(service);
        AddSingleton<CustomKnifeCoordinator>(service);
        AddSingleton<IKnifeService, KnifeService>(service);
        AddSingleton<IKnivesRegistry, KnivesRegistry>(service);
        AddSingleton<IWritableKnivesRegistry>(service, provider =>
            (IWritableKnivesRegistry)provider.GetRequiredService<IKnivesRegistry>()
        );
        AddSingleton<KnifeRegistryInitializer>(service);
        AddSingleton<KnifeCatalogSynchronizer>(service);
        AddSingleton<MenuApiBridge>(service);
        AddSingleton<IPlayerKnifePersistenceService, PlayerKnifePersistenceService>(service);
        AddSingleton<PlayerSessionStore<PlayerKnifePreferences>>(service);
        AddSingleton<IPlayerKnifeService, PlayerKnifeService>(service);
        
        AddSingleton<IMenuExtensionDispatcher>(service, provider => provider.GetRequiredService<MenuApiBridge>());
    }

    private void BuildKnives(ServiceCollection service)
    {
        AddTransient<IKnife, Spike>(service);
        AddTransient<IKnife, Piercer>(service);
        AddTransient<IKnife, Axe>(service);
        AddTransient<IKnife, Katana>(service);
    }
    
    private static void BuildKnifeConfigs(ServiceCollection service)
    {
        service.AddSingleton<SpikeConfig>(provider => 
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.SpikeConfig
        );
        service.AddSingleton<PiercerConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.PiercerConfig
        );
        service.AddSingleton<AxeConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.AxeConfig
        );
        service.AddSingleton<KatanaConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.KatanaConfig
        );
    }
    
    private void BuildDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "custom_knife",
            Schema = CustomKnifeDbContext.SchemaName,

            CommandTimeoutSeconds = 5,

            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };
        
        service.AddPostgreSqlDatabase<CustomKnifeDbContext>(core, options);
        service.AddSteamEntityStore<CustomKnifeDbContext, PlayerKnifeEntity>();
        AddSingleton<IKnifeCatalogRepository, KnifeCatalogRepository>(service);
    }
}
