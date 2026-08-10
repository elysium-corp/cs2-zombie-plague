using Common.Di.SharedInterfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Di.Utils;

public static class SharedInterfaceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedInterface<TInterface>() where TInterface : class
        {
            services.AddSingleton<SharedInterfaceReference<TInterface>>();

            services.AddSingleton<TInterface>(static provider =>
                provider.GetRequiredService<SharedInterfaceReference<TInterface>>().Value
            );

            return services;
        }
    }
}