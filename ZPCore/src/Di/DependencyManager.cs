using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZPApi.Events;
using ZPCore.Config.Ability;
using ZPCore.Config.Core;
using ZPCore.Config.Round;
using ZPCore.Config.Zombie;
using ZPCore.Data;
using ZPCore.Data.Abilities;
using ZPCore.Data.Abilities.Contracts;
using ZPCore.Data.Effects;
using ZPCore.Data.Effects.Contracts;
using ZPCore.Data.Events;
using ZPCore.Data.Lifecycle;
using ZPCore.Data.Managers;
using ZPCore.Data.Menus;
using ZPCore.Data.Plugins.ResourceLoader;
using ZPCore.Data.Rounds.Contracts;
using ZPCore.Data.Weapons;
using ZPCore.Data.Zombies;
using ZPCore.Data.Zombies.ZClasses;
using ZPCore.Service;

namespace ZPCore.Di;

internal static class DependencyManager

{
    private static IServiceCollection? _services;
    private static ServiceProvider? _provider;
    
    private const string RoundConfigName = "round.json";
    private const string RoundConfigSectionName = "RoundConfig";
    
    private const string AbilityConfigName = "ability.json";
    private const string AbilityConfigSectionName = "AbilityConfig";
    
    private const string ZClassConfigName = "zombie_class.json";
    private const string ZClassConfigSectionName = "ZClassConfig";
    
    private const string KnifeConfigName = "knife.json";
    private const string KnifeConfigSectionName = "KnifeConfig";
    
    private const string CoreConfigName = "core.json";
    private const string CoreConfigSectionName = "CoreConfig";

    public static void Load(ISwiftlyCore core)
    {
        core.Configuration
            .InitializeJsonWithModel<RoundConfig>(RoundConfigName, RoundConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(RoundConfigName, optional: false, reloadOnChange: true); });
        
        core.Configuration
            .InitializeJsonWithModel<AbilityConfig>(AbilityConfigName, AbilityConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(AbilityConfigName, optional: false, reloadOnChange: true); });

        core.Configuration
            .InitializeJsonWithModel<ZClassConfig>(ZClassConfigName, ZClassConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(ZClassConfigName, optional: false, reloadOnChange: true); });
        
        core.Configuration
            .InitializeJsonWithModel<ZombiePlagueCoreConfig>(CoreConfigName, CoreConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(CoreConfigName, optional: false, reloadOnChange: true); });

        _services = new ServiceCollection();

        _services
            .AddSwiftly(core)
            .AddSingleton<IResourceLoader, ResourceLoader>()
            .AddSingleton<IRoundFactory, RoundFactory>()
            .AddSingleton<IZombieFactory, ZombieFactory>()
            .AddSingleton<IAbilityFactory, AbilityFactory>()
            .AddSingleton<IEffectFactory, EffectFactory>()
            .AddSingleton<IWeaponFactory, WeaponFactory>()
            .AddSingleton<IGrenadeFactory, GrenadeFactory>()
            .AddSingleton<IWeaponRegistrator, WeaponRegistrator>()
            .AddSingleton<ICustomEventService, CustomEventsService>()
            .AddSingleton<LifecycleManager>()
            .AddSingleton<PlayerLifecycleManager>()
            .AddSingleton<ServiceLifecycleManager>()
            .AddSingleton<ZClassMenu>()
            .AddSingleton<ZombieManager>()
            .AddSingleton<EffectManager>()
            .AddSingleton<RoundManager>()
            .AddSingleton<HumanManager>()
            .AddSingleton<Knockback>()
            .AddSingleton<WeaponService>()
            .AddSingleton<WeaponParticleService>();
        
        _services
            .AddOptionsWithValidateOnStart<RoundConfig>()
            .BindConfiguration(RoundConfigSectionName);
        
        _services
            .AddOptionsWithValidateOnStart<AbilityConfig>()
            .BindConfiguration(AbilityConfigSectionName);

        _services
            .AddOptionsWithValidateOnStart<ZClassConfig>()
            .BindConfiguration(ZClassConfigSectionName);
        
        _services
            .AddOptionsWithValidateOnStart<ZombiePlagueCoreConfig>()
            .BindConfiguration(CoreConfigSectionName);
        
        _services
            .AddSingleton<EventService>()
            .AddSingleton<IEventSubscriber>(sp => sp.GetRequiredService<EventService>())
            .AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<EventService>());

        RegisterZClasses();
        
        _provider = _services.BuildServiceProvider();
    }

    public static void Dispose()
    {
        _provider?.Dispose();
        _services = null;
    }

    public static T GetService<T>() where T : notnull
    {
        return _provider == null
            ? throw new NoNullAllowedException(Tag + " _provider is null!")
            : _provider.GetRequiredService<T>();
    }
    
    private static void RegisterZClasses()
    {
        if (_services == null)
        {
            throw new NoNullAllowedException(Tag + " _services is null!");
        }
        
        _services
            .AddTransient<ZCleric>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Cleric;
                return new ZCleric(config, abilityFactory);
            });

        _services
            .AddTransient<ZHunter>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Hunter;
                return new ZHunter(config, abilityFactory);
            });
        
        _services
            .AddTransient<ZAssassin>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Assassin;
                return new ZAssassin(config, abilityFactory);
            });
        
        _services
            .AddTransient<ZHeavy>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Heavy;
                return new ZHeavy(config, abilityFactory);
            });
        
        _services
            .AddTransient<ZSmoker>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Smoker;
                return new ZSmoker(config, abilityFactory);
            });
        
        _services
            .AddTransient<ZNemesis>(sp =>
            {
                var abilityFactory = sp.GetRequiredService<IAbilityFactory>();
                var zClassConfig = sp.GetRequiredService<IOptions<ZClassConfig>>().Value;
                var config = zClassConfig.Nemesis;
                return new ZNemesis(config, abilityFactory);
            });
    }

    private const string Tag = "DependencyManager";
}