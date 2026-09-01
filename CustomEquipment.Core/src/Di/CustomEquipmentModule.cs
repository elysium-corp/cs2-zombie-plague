using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Common.Di.Utils;
using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Registration;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Catalog;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Database;
using CustomEquipment.Fetcher;
using CustomEquipment.Fetcher.Analyzers;
using CustomEquipment.Giver;
using CustomEquipment.Menus;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using Economy.Api;
using Localization.Api;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace CustomEquipment.Di;

internal sealed class CustomEquipmentModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        service.AddSharedInterface<IEconomyApi>();
        service.AddSharedInterface<IZombiePlagueApi>();
        service.AddSharedInterface<ILocalizationApi>();

        AddSingleton<HookService>(service);
        AddSingleton<IHookSubscriber>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<IHookPublisher>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<CustomEquipmentItemEvents>(service);
        AddSingleton<CustomEquipmentWeaponEvents>(service);
        AddSingleton<CustomEquipmentGrenadeEvents>(service);
        AddSingleton<CustomEquipmentMineEvents>(service);
        AddSingleton<ICustomEquipmentEvents, CustomEquipmentEvents>(service);

        AddSingleton<IEquipmentFetcher>(service, OnWeaponRegistratorFactory);
        AddSingleton<IEquipmentService, EquipmentService>(service);
        AddSingleton<IParticleService, ParticleService>(service);
        AddSingleton<IWeaponController, WeaponController>(service);
        AddSingleton<ILaserMineInstallerService, LaserMineInstallerService>(service);
        AddSingleton<IMineController, MineController>(service);
        AddSingleton<IWeaponSoundController, WeaponSoundController>(service);
        AddSingleton<IEquipmentShopCatalog, EquipmentShopCatalog>(service);
        AddSingleton<GameplayItemCatalog>(service);
        AddSingleton<EquipmentCatalogSynchronizer>(service);
        AddSingleton<IItemGiver, ItemGiver>(service);
        AddSingleton<ItemRegistry>(service);
        AddSingleton<IItemRegistry>(service, provider => provider.GetRequiredService<ItemRegistry>());
        AddSingleton<IEquipmentRegistrar>(service, provider => provider.GetRequiredService<ItemRegistry>());

        AddSingleton<EquipmentMenu>(service);
        AddSingleton<CustomEquipmentApi>(service);

        AddDatabase(service);

        return (service.BuildServiceProvider(), service);
    }

    private void AddDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = CustomEquipmentDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };

        service.AddPostgreSqlDatabase<CustomEquipmentDbContext>(Core, options);
        AddSingleton<IWeaponCatalogRepository, WeaponCatalogRepository>(service);
        AddSingleton<IGameplayItemCatalogRepository, GameplayItemCatalogRepository>(service);
    }

    private IEquipmentFetcher OnWeaponRegistratorFactory(IServiceProvider service)
    {
        var compileAnalyzer = new CompileAnalyzer<IItem>(typeof(CustomEquipmentModule).Assembly, service);
        return new EquipmentFetcher(compileAnalyzer);
    }
}
