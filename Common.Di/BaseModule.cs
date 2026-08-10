using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftlyS2.Shared;

namespace Common.Di;

public abstract class BaseModule(ISwiftlyCore core) : IModule
{
    public abstract (ServiceProvider, ServiceCollection) GetProvider();
    
    protected IServiceCollection AddConfig<TConfig>(ServiceCollection service, string name, string section, bool optional = false, bool reloadOnChange = true) where TConfig : class, new()
    {
        var configuration = core.Configuration
            .InitializeJsonWithModel<TConfig>(name, section)
            .Configure(builder => { builder.AddJsonFile(name, optional: optional, reloadOnChange: reloadOnChange); });

        var configurationManager = configuration.Manager;

        service.TryAddSingleton(configuration);
        service.TryAddSingleton(configurationManager);
        service.TryAddSingleton<IConfiguration>(configurationManager);
        
        service
            .AddOptionsWithValidateOnStart<TConfig>()
            .BindConfiguration(section);

        return service;
    }

    protected IServiceCollection AddSingleton<TService>(ServiceCollection service) where TService : class
    {
        return service.AddSingleton<TService>();
    }

    protected IServiceCollection AddSingleton<TInterface, TImplementation>(ServiceCollection service)
        where TInterface : class where TImplementation : class, TInterface
    {
        return service.AddSingleton<TInterface, TImplementation>();
    }

    protected IServiceCollection AddSingleton<TService>(ServiceCollection service, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        return service.AddSingleton(factory);
    }
    
    protected IServiceCollection AddSingleton(ServiceCollection service, Type type)
    {
        return service.AddSingleton(type);
    }
    
    protected IServiceCollection AddTransient<TService>(ServiceCollection service) where TService : class
    {
        return service.AddTransient<TService>();
    }
    
    protected IServiceCollection AddTransient<TInterface, TImplementation>(ServiceCollection service)
        where TInterface : class where TImplementation : class, TInterface
    {
        return service.AddTransient<TInterface, TImplementation>();
    }
    
    protected IServiceCollection AddTransient(ServiceCollection service, Type serviceType, Type implementationType)
    {
        return service.AddTransient(serviceType, implementationType);
    }
}
