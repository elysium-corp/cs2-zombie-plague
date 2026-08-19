using Common.Database;
using Common.Database.Storages;
using Common.Database.Utils;
using Common.Di;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Menu.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Api.Events;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Controllers;
using ZombiePlague.Core.Data.Coordinators;
using ZombiePlague.Core.Data.Coordinators.Contracts;
using ZombiePlague.Core.Data.Entities.Human.Factory;
using ZombiePlague.Core.Data.Entities.Registrator;
using ZombiePlague.Core.Data.Entities.Zombie.Factory;
using ZombiePlague.Core.Data.Events;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Data.Service;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Database;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Menus;
using ZombiePlague.Core.Store.Data;
using ZombiePlague.Core.Store.Repository;

namespace ZombiePlague.Core.Di;

public sealed class ZombiePlagueModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        BuildConfigs(service);
        BuildSingletons(service);
        AddDatabase(service);

        var provider = service.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return (provider, service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<ZombiePlagueCoreConfig>(
            service: service,
            name: "core.json",
            section: "CoreConfig"
        );
        AddConfig<ZClassConfig>(
            service: service,
            name: "zombie_class.json",
            section: "ZClassConfig"
        );
        AddConfig<HClassConfig>(
            service: service,
            name: "human_class.json",
            section: "HClassConfig"
        );
        AddConfig<AbilityConfig>(
            service: service,
            name: "ability.json",
            section: "AbilityConfig"
        );
        AddConfig<RoundConfig>(
            service: service,
            name: "round.json",
            section: "RoundConfig"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<HookService>(service);
        AddSingleton<IHookSubscriber>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<IHookPublisher>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<ZombiePlaguePreEvents>(service);
        AddSingleton<ZombiePlaguePostEvents>(service);
        AddSingleton<ZombiePlagueEvents>(service);

        AddSingleton<IResourceLoader, ResourceLoader>(service);
        AddSingleton<ICustomEventService, CustomEventsService>(service);

        AddSingleton<PlayerSessionStore<PlayerPreferences>>(service);
        AddSingleton<IPlayerRepository, PlayerRepository>(service);
        AddSingleton<IPlayerPersistenceService, PlayerPersistenceService>(service);
        AddSingleton<IPlayerPreferencesCoordinator, PlayerPreferencesCoordinator>(service);

        AddSingleton<IAbilityFactory, AbilityFactory>(service);
        AddSingleton<IHClassFactory, HClassFactory>(service);
        AddSingleton<IZClassFactory, ZClassFactory>(service);

        AddSingleton<HumanController>(service);
        AddSingleton<ZombieController>(service);

        AddSingleton<IPlayerManager, PlayerManager>(service);
        AddSingleton<IRoundFactory, RoundFactory>(service);
        AddSingleton<IRoundManager, RoundManager>(service);
        AddSingleton<IRoundRegistrator, RoundRegistrator>(service);
        AddSingleton<IZClassRegistrator, ZClassRegistrator>(service);

        AddSingleton<IPlayerService, PlayerService>(service);
        AddSingleton<IMapService, MapService>(service);
        AddSingleton<IRoundService, RoundService>(service);
        AddSingleton<IInfectionService, InfectionService>(service);
        AddSingleton<IKnockbackService, KnockbackService>(service);
        AddSingleton<ICommandService, CommandService>(service);
        AddSingleton<IZombiePlagueCoordinator, ZombiePlagueCoordinator>(service);

        AddSingleton<MenuExtensionDispatcherProxy>(service);
        AddSingleton<IMenuExtensionDispatcher>(
            service,
            provider => provider.GetRequiredService<MenuExtensionDispatcherProxy>()
        );
        AddSingleton<MainMenu>(service);
        AddSingleton<ZClassMenu>(service);

        AddSingleton<ZombiePlagueApi>(service);
    }

    private void AddDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = ZombiePlagueDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };

        service.AddPostgreSqlDatabase<ZombiePlagueDbContext>(core, options);
        service.AddSteamEntityStore<ZombiePlagueDbContext, PlayerEntity>();
    }
}
