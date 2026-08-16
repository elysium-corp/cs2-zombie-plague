using System.Reflection;
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
using CustomKnife.Initializer;
using Menu.Api.Extensions;
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
        BuildTransients(service);

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
        
        AddSingleton<KnifeMenu>(service);
        AddSingleton<CustomKnifeCoordinator>(service);
        AddSingleton<IKnifeService, KnifeService>(service);
        AddSingleton<IKnivesRegistry, KnivesRegistry>(service);
        AddSingleton<KnifeRegistryInitializer>(service);
        AddSingleton<MenuApiBridge>(service);
        AddSingleton<IPlayerKnifePersistenceService, PlayerKnifePersistenceService>(service);
        AddSingleton<PlayerKnifeStore>(service);
        AddSingleton<IPlayerKnifeService, PlayerKnifeService>(service);
        
        AddSingleton<IMenuExtensionDispatcher>(service, provider => provider.GetRequiredService<MenuApiBridge>());
    }

    private void BuildTransients(ServiceCollection service)
    {
        var baseType = typeof(IKnife);

        var knives = Assembly.GetAssembly(baseType)!
            .GetTypes()
            .Where(type => type.IsClass
                           && !type.IsAbstract
                           && baseType.IsAssignableFrom(type));

        foreach (var knife in knives)
        {
            AddTransient(service, baseType, knife);
        }
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
        using var connection = core.Database.GetConnection("custom_knife");

        var connectionString = connection.ConnectionString;

        service.AddDbContextFactory<CustomKnifeDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        CustomKnifeDbContext.SchemaName
                    );

                    npgsql.CommandTimeout(5);
                }
            );
        });
    }
}