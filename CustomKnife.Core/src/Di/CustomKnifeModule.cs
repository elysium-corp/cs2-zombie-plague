using System.Reflection;
using Common.Di;
using CustomKnife.Data.Configs;
using CustomKnife.Data.Menus;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Initializer;
using Menu.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace CustomKnife.Di;

internal sealed class CustomKnifeModule(ISwiftlyCore core) : BaseModule(core)
{

    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

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
            name: "knifes.json",
            section: "KnifeConfig"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<KnifeMenu>(service);
        AddSingleton<CustomKnifeCoordinator>(service);
        AddSingleton<IKnifeService, KnifeService>(service);
        AddSingleton<IKnivesRegistry, KnivesRegistry>(service);
        AddSingleton<KnifeRegistryInitializer>(service);
        AddSingleton<MenuApiBridge>(service);
        AddSingleton<IMenuExtensionDispatcher>(service, provider => 
            provider.GetRequiredService<MenuApiBridge>()
        );
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
        service.AddSingleton<AncientConfig>(provider => 
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.AncientConfig
        );
        service.AddSingleton<MonarchConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.MonarchConfig
        );
        service.AddSingleton<GaiasVengeanceConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.GaiasVengeanceConfig
        );
        service.AddSingleton<KatanaConfig>(provider =>
            provider.GetRequiredService<IOptions<KnifeConfig>>().Value.KatanaConfig
        );
    }
}