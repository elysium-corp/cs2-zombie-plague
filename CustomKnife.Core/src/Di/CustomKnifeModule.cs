using System.Reflection;
using Common.Di;
using CustomKnife.Data.Configs;
using CustomKnife.Data.Models;
using CustomKnife.Data.Services;
using CustomKnife.Data.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace CustomKnife.Di;

internal sealed class CustomKnifeModule(ISwiftlyCore core) : BaseModule(core)
{
    private readonly ISwiftlyCore _core = core;

    public override ServiceProvider GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(_core);

        BuildConfigs(service);
        BuildSingletons(service);
        BuildTransients(service);

        return service.BuildServiceProvider();
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
        AddSingleton<IKnifeService, KnifeService>(service);
        AddSingleton<IKnifeMenuService, KnifeMenuService>(service);

        var baseType = typeof(IKnifeConfig);

        var knifeConfigs = Assembly.GetAssembly(baseType)!
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
            .Select(type => (IKnifeConfig)Activator.CreateInstance(type)!);

        foreach (var knifeConfig in knifeConfigs)
        {
            AddSingleton(service, knifeConfig.GetType());
        }
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
}