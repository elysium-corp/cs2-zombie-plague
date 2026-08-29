using Common.Di.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Common.Di;

internal static class DependencyManager
{
    private static readonly Lock Sync = new();
    private static readonly Dictionary<Type, (ServiceProvider, ServiceCollection)> Providers = [];
    private static ServiceProvider[] _snapshot = [];
    
    internal static ServiceProvider[] GetProviders() => Volatile.Read(ref _snapshot);

    internal static ServiceProvider GetRequiredProvider<TModule>() where TModule : IModule
    {
        lock (Sync)
        {
            if (!Providers.TryGetValue(typeof(TModule), out var provider))
                throw new ServiceNotFoundException($"Provider for module '{typeof(TModule).Name}' not found");
            return provider.Item1;
        }
    }

    internal static TModule BuildModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        var module = CreateModule<TModule>(core);
        var (provider, service) = module.GetProvider();

        var duplicate = false;
        lock (Sync)
        {
            if (Providers.ContainsKey(typeof(TModule))) duplicate = true;
            else
            {
                Providers.Add(typeof(TModule), (provider, service));
                PublishSnapshot();
            }
        }

        if (duplicate)
        {
            provider.Dispose();
            throw new InvalidOperationException($"Provider for module '{typeof(TModule).Name}' is already registered.");
        }
        
        return module;
    }
    
    internal static bool DestroyModule<TModule>() where TModule : IModule
    {
        (ServiceProvider, ServiceCollection) provider;
        lock (Sync)
        {
            if (!Providers.Remove(typeof(TModule), out provider)) return false;
            PublishSnapshot();
        }

        provider.Item1.Dispose();

        return true;
    }

    private static void PublishSnapshot() =>
        Volatile.Write(ref _snapshot, Providers.Values.Select(static value => value.Item1).ToArray());

    private static TModule CreateModule<TModule>(ISwiftlyCore core) where TModule : IModule
    {
        return (TModule?)Activator.CreateInstance(typeof(TModule), core) ??
               throw new ModuleNotCreatedException(typeof(TModule).Name + ": not create or failed!");
    }
}
