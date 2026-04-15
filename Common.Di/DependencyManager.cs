using Common.Di.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Common.Di;

internal static class DependencyManager
{
    private static readonly Dictionary<Type, (ServiceProvider, ServiceCollection)> Providers = [];

    internal static bool Loaded;
    
    internal static void GetAllDependencies()
    {
        Loaded = true;
        Console.WriteLine($"=================== DEPENDENCIES ===================\n");
        foreach (var provider in Providers)
        {
            var module = provider.Key.Name;
            var dependencies = provider.Value.Item2.Count;
            var ix = 1;
            Console.WriteLine($"Module: {module} [{dependencies} dependencies]\n");
            foreach (var dependency in provider.Value.Item2)
            {
                Console.WriteLine($"{ix}.\t[{dependency.Lifetime}]\t{dependency.ServiceType.Name}");
                ix++;
            }
            Console.WriteLine($"\n");
        }
        Console.WriteLine($"======================= END ========================");
    }
    
    internal static List<ServiceProvider> GetProviders()
    {
        return Providers.Values.Select(pair => pair.Item1).ToList();
    }

    internal static ServiceProvider GetRequiredProvider<TModule>() where TModule : IModule
    {
        if (!Providers.TryGetValue(typeof(TModule), out var provider)) throw new ServiceNotFoundException($"Provider for module '{typeof(TModule).Name}' not found");
        
        return provider.Item1;
    }

    internal static TModule BuildModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        var module = CreateModule<TModule>(core);
        var (provider, service) = module.GetProvider();

        Providers[typeof(TModule)] = (provider, service);
        
        return module;
    }
    
    internal static bool DestroyModule<TModule>() where TModule : IModule
    {
        if (!Providers.Remove(typeof(TModule), out var provider)) return false;

        provider.Item1.Dispose();

        return true;
    }

    private static TModule CreateModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        return (TModule?)Activator.CreateInstance(typeof(TModule), core) ??
               throw new ModuleNotCreatedException(typeof(TModule).Name + ": not create or failed!");
    }
}