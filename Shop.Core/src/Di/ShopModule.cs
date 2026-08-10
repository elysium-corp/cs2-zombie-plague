using Common.Di;
using Common.Di.Utils;
using CustomEquipment.Api;
using Menu.Api;
using Menu.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MoneySystem.Api;
using Shop.Core.Data.Configs;
using Shop.Core.Menus;
using Shop.Core.Services;
using Shop.Core.SharedApi;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace Shop.Core.Di;

internal sealed class ShopModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();

        services.AddSwiftly(core);

        BuildConfig(services);
        BuildSharedInterfaces(services);
        BuildServices(services);

        return (services.BuildServiceProvider(), services);
    }

    private void BuildConfig(ServiceCollection services)
    {
        AddConfig<ShopConfig>(
            services,
            name: "shop.json",
            section: "Shop"
        );

        services.AddOptions<ShopConfig>()
            .Validate(
                static config => config.Prices is not null && config.Items is not null,
                "Shop prices and item overrides must be configured."
            )
            .Validate(
                static config => HasValidPrices(config),
                "Shop prices cannot be negative."
            );
    }

    private static bool HasValidPrices(ShopConfig config)
    {
        if (config.Prices is null || config.Items is null)
        {
            return false;
        }

        return Enum.GetValues<CustomEquipment.Api.Data.EquipmentCategory>()
                   .All(category => config.GetDefaultPrice(category) >= 0)
               && config.Items.Values.All(item => !item.Price.HasValue || item.Price.Value >= 0);
    }

    private void BuildSharedInterfaces(ServiceCollection services)
    {
        services.AddSharedInterface<ICustomEquipmentApi>();
        services.AddSharedInterface<IMenuApi>();
        services.AddSharedInterface<IMoneySystemPaymentApi>();
        services.AddSharedInterface<IZombiePlagueApi>();

        AddSingleton<IMenuExtensionDispatcher>(
            services,
            provider => provider.GetRequiredService<IMenuApi>().Dispatcher
        );
    }

    private void BuildServices(ServiceCollection services)
    {
        AddSingleton<IShopCatalog, ShopCatalog>(services);
        AddSingleton<IShopAccessPolicy, ShopAccessPolicy>(services);
        AddSingleton<IShopPurchaseService, ShopPurchaseService>(services);
        AddSingleton<ShopCategoryMenu>(services);
        AddSingleton<ShopMenu>(services);
        AddSingleton<ShopCoordinator>(services);

        AddSingleton<ShopApi>(services, provider => new ShopApi(
            new Lazy<IShopCatalog>(() => provider.GetRequiredService<IShopCatalog>()),
            new Lazy<IShopPurchaseService>(() => provider.GetRequiredService<IShopPurchaseService>()),
            new Lazy<ShopMenu>(() => provider.GetRequiredService<ShopMenu>())
        ));
    }
}
