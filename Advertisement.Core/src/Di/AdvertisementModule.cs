using Advertisement.Core.Application;
using Advertisement.Core.Api;
using Advertisement.Core.Configuration;
using Advertisement.Core.Data;
using Advertisement.Core.Database;
using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Advertisement.Core.Di;

internal sealed class AdvertisementModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        AddConfig<AdvertisementConfig>(services, "advertisement.json", "AdvertisementConfig");
        services.AddSwiftly(Core);

        AddSingleton<AdvertisementCache>(services);
        AddSingleton<PlayerLocaleStore>(services);
        AddSingleton<PlayerLocaleResolver>(services);
        AddSingleton<AdminAudienceResolver>(services);
        AddSingleton<MarkupRenderer>(services);
        AddSingleton<PlaceholderResolver>(services);
        AddSingleton<AdvertisementSender>(services);
        AddSingleton<AdvertisementScheduler>(services);
        AddSingleton<AdvertisementApi>(services);
        AddSingleton<ConfigAdvertisementProvider>(services);
        AddSingleton<DatabaseAdvertisementProvider>(services);
        AddSingleton<PlayerPreferenceRepository>(services);
        AddSingleton<RateLimitedLogger>(services, _ => new RateLimitedLogger(Core.Logger));
        AddSingleton<AdvertisementCoordinator>(services, provider => new AdvertisementCoordinator(
            provider.GetRequiredService<AdvertisementCache>(),
            provider.GetRequiredService<DatabaseAdvertisementProvider>(),
            provider.GetRequiredService<ConfigAdvertisementProvider>(),
            provider.GetRequiredService<RateLimitedLogger>(),
            Core.Logger));

        services.AddPostgreSqlDatabase<AdvertisementDbContext>(Core, new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = AdvertisementDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3),
        });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        return (provider, services);
    }
}
