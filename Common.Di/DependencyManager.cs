using Common.Di.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Common.Di;

internal static class DependencyManager
{
    private static readonly Dictionary<Type, ServiceProvider> Providers = [];

    internal static List<ServiceProvider> GetProviders()
    {
        return Providers.Values.ToList();
    }

    internal static ServiceProvider GetRequiredProvider<TModule>() where TModule : IModule
    {
        if (!Providers.TryGetValue(typeof(TModule), out var provider)) throw new ServiceNotFoundException($"Provider for module '{typeof(TModule).Name}' not found");
        
        return provider;
    }

    internal static TModule BuildModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        var module = CreateModule<TModule>(core);
        var provider = module.GetProvider();

        Providers[typeof(TModule)] = provider;
        
        return module;
    }
    
    internal static bool DestroyModule<TModule>() where TModule : IModule
    {
        if (!Providers.Remove(typeof(TModule), out var provider)) return false;

        provider.Dispose();

        return true;
    }

    private static TModule CreateModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        return (TModule?)Activator.CreateInstance(typeof(TModule), core) ??
               throw new ModuleNotCreatedException(typeof(TModule).Name + ": not create or failed!");
    }
}