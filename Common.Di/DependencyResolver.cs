using Common.Di.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Di;

public static class DependencyResolver
{
    private const string Tag = "DependencyResolver";
    
    public static TService GetRequiredService<TService>() where TService : notnull
    {
        var providers = DependencyManager.GetProviders();

        foreach (var provider in providers)
        {
            var service = provider.GetService(typeof(TService));
            
            if (service == null) continue;

            return (TService)service;
        }
        
        throw new ServiceNotFoundException($"{Tag}: GetRequiredService not found service!");
    }

    public static TService GetRequiredService<TModule, TService>() where TModule : IModule where TService : notnull
    {
        var provider = DependencyManager.GetRequiredProvider<TModule>();
        var service = provider.GetRequiredService<TService>();

        return service;
    }

    public static Lazy<TService> GetRequiredServiceLazy<TService>() where TService : notnull
    {
        return new Lazy<TService>(GetRequiredService<TService>);
    }
    
    public static Lazy<TService> GetRequiredServiceLazy<TModule, TService>() where TModule : IModule where TService : notnull
    {
        return new Lazy<TService>(GetRequiredService<TModule, TService>);
    }
}