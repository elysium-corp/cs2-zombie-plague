using Admin.Api;
using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Common.Di.Utils;
using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api;
using Economy.Api;
using Localization.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shop.Api.Events;
using Shop.Core.Api;
using Shop.Core.Application;
using Shop.Core.Configuration;
using Shop.Core.Data;
using Shop.Core.Database;
using Shop.Core.Menus;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace Shop.Core.Di;

internal sealed class ShopModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        Core.Configuration.Configure(builder => builder.AddJsonFile(source =>
        {
            source.Path = "shop.json";
            source.Optional = true;
            // Провайдер перечитывает файл, но runtime snapshot заменяется только координатором:
            // в конце карты или по команде shop_reload.
            source.ReloadOnChange = true;
            source.OnLoadException = context =>
            {
                Core.Logger.LogError(
                    context.Exception,
                    "[Shop] shop.json повреждён и проигнорирован. " +
                    "При доступном PostgreSQL будет загружен обычный memory snapshot.");
                context.Ignore = true;
            };
        }));
        services.AddOptionsWithValidateOnStart<ShopFallbackConfig>()
            .BindConfiguration(string.Empty);
        services.AddSwiftly(Core);
        services.AddSharedInterface<ICustomEquipmentApi>();
        services.AddSharedInterface<IEconomyApi>();
        services.AddSharedInterface<ILocalizationApi>();
        services.AddSharedInterface<IZombiePlagueApi>();

        AddSingleton<HookService>(services);
        AddSingleton<IHookSubscriber>(services, provider => provider.GetRequiredService<HookService>());
        AddSingleton<IHookPublisher>(services, provider => provider.GetRequiredService<HookService>());
        AddSingleton<ShopEvents>(services);
        AddSingleton<IShopEvents>(services, provider => provider.GetRequiredService<ShopEvents>());
        AddSingleton<ShopAdminApiProxy>(services);
        AddSingleton<IAdminApi>(services, provider => provider.GetRequiredService<ShopAdminApiProxy>());
        AddSingleton<ShopSnapshotCache>(services);
        AddSingleton<FallbackShopSnapshotProvider>(services);
        AddSingleton<ShopSnapshotRepository>(services);
        AddSingleton<ShopSnapshotCoordinator>(services);
        AddSingleton<ShopPurchaseCounter>(services);
        AddSingleton<ShopProductProvider>(services);
        AddSingleton<ShopAccessEvaluator>(services);
        AddSingleton<ShopPurchaseService>(services);
        AddSingleton<ShopMenu>(services);
        AddSingleton<ShopApi>(services);

        services.AddPostgreSqlDatabase<ShopDbContext>(Core, new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = ShopDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        return (provider, services);
    }
}
