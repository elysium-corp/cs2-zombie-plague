using System.Data;
using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.Ability;
using CS2ZombiePlague.Config.models;
using CS2ZombiePlague.Config.Weapon;
using CS2ZombiePlague.Config.Zombie;
using CS2ZombiePlague.Data;
using CS2ZombiePlague.Data.Abilities;
using CS2ZombiePlague.Data.Abilities.Contracts;
using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Lifecycle;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.Weapons;
using CS2ZombiePlague.Data.Weapons.Knifes;
using CS2ZombiePlague.Data.ZClasses;
using CS2ZombiePlague.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Di;

public static class DependencyManager
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
    private const string ModelConfigName = "models.json";
    private const string ModelConfigSectionName = "ModelConfig";

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
            .InitializeJsonWithModel<KnifeConfig>(KnifeConfigName, KnifeConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(KnifeConfigName, optional: false, reloadOnChange: true); });
        
        core.Configuration
            .InitializeJsonWithModel<ZombiePlagueCoreConfig>(CoreConfigName, CoreConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(CoreConfigName, optional: false, reloadOnChange: true); });
        
        core.Configuration
            .InitializeJsonWithModel<ModelsConfig>(ModelConfigName, ModelConfigSectionName)
            .Configure(builder => { builder.AddJsonFile(ModelConfigName, optional: false, reloadOnChange: true); });

        _services = new ServiceCollection();

        _services
            .AddSwiftly(core)
            .AddSingleton<IRoundFactory, RoundFactory>()
            .AddSingleton<IZombiePlayerFactory, ZombiePlayerFactory>()
            .AddSingleton<IKnifeFactory, KnifeFactory>()
            .AddSingleton<IAbilityFactory, AbilityFactory>()
            .AddSingleton<IWeaponFactory, WeaponFactory>()
            .AddSingleton<LifecycleManager>()
            .AddSingleton<PlayerLifecycleManager>()
            .AddSingleton<ServiceLifecycleManager>()
            .AddSingleton<ZClassMenu>()
            .AddSingleton<ZombieManager>()
            .AddSingleton<KnifeManager>()
            .AddSingleton<RoundManager>()
            .AddSingleton<HumanManager>()
            .AddSingleton<Knockback>()
            .AddSingleton<WeaponManager>()
            .AddSingleton<DamageNotify>()
            .AddSingleton<MoneySystem>()
            .AddSingleton<ScreenFade>()
            .AddSingleton<WeaponService>()
            .AddSingleton<RoundRatingNotify>()
            .AddSingleton<WeaponParticleService>()
            .AddSingleton<CommonUtils>();
        
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
            .AddOptionsWithValidateOnStart<KnifeConfig>()
            .BindConfiguration(KnifeConfigSectionName);
        
        _services
            .AddOptionsWithValidateOnStart<ZombiePlagueCoreConfig>()
            .BindConfiguration(CoreConfigSectionName);
        
        _services
            .AddOptionsWithValidateOnStart<ModelsConfig>()
            .BindConfiguration(ModelConfigSectionName);
        
        // EventService Registration 
        _services.AddSingleton<EventService>();
        _services.AddSingleton<IEventSubscriber>(sp =>
            sp.GetRequiredService<EventService>());
        _services.AddSingleton<IEventPublisher>(sp =>
            sp.GetRequiredService<EventService>());

        RegisterZClasses();
        RegisterKnifes();
        
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

    private static void RegisterKnifes()
    {
        if (_services == null)
        {
            throw new NoNullAllowedException(Tag + " _services is null!");
        }
        
        _services
            .AddTransient<KnockbackKnifeWeapon>(sp =>
            {
                var knifeConfig = sp.GetRequiredService<IOptions<KnifeConfig>>().Value;
                var config = knifeConfig.Knockback;
                return new KnockbackKnifeWeapon(config);
            });
        _services
            .AddTransient<SpeedKnifeWeapon>(sp =>
            {
                var knifeConfig = sp.GetRequiredService<IOptions<KnifeConfig>>().Value;
                var config = knifeConfig.Speed;
                return new SpeedKnifeWeapon(config);
            });
        _services
            .AddTransient<GravityKnifeWeapon>(sp =>
            {
                var knifeConfig = sp.GetRequiredService<IOptions<KnifeConfig>>().Value;
                var config = knifeConfig.Gravity;
                return new GravityKnifeWeapon(config);
            });
        _services
            .AddTransient<VipKnifeWeapon>(sp =>
            {
                var knifeConfig = sp.GetRequiredService<IOptions<KnifeConfig>>().Value;
                var config = knifeConfig.Vip;
                return new VipKnifeWeapon(config);
            });
        
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