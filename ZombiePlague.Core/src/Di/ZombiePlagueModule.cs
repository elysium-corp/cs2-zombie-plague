using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Events;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Menus;
using ZombiePlague.Core.Data.Menus.Contracts;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Zombies;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZPCore.Config.Core;
using ZPCore.Config.Zombie;

namespace ZombiePlague.Core.Di;

public sealed class ZombiePlagueModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        BuildConfigs(service);
        BuildSingletons(service);

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
        AddSingleton<IResourceLoader, ResourceLoader>(service);
        AddSingleton<IRoundFactory, RoundFactory>(service);
        AddSingleton<IZombieFactory, ZombieFactory>(service);
        AddSingleton<IZClassFactory, ZClassFactory>(service);
        AddSingleton<IAbilityFactory, AbilityFactory>(service);
        AddSingleton<ICustomEventService, CustomEventsService>(service);
        AddSingleton<IZClassMenu, ZClassMenu>(service);
        AddSingleton<IZombieManager, ZombieManager>(service);
        AddSingleton<IHumanManager, HumanManager>(service);
        AddSingleton<IKnockback, Knockback>(service);
        AddSingleton<RoundManager>(service);
        AddSingleton<EventService>(service);
        AddSingleton<IEventSubscriber>(service, s => s.GetRequiredService<EventService>());
        AddSingleton<IEventPublisher>(service, s => s.GetRequiredService<EventService>());
    }
}