using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Localization.Api;
using Localization.Core.Api;
using Localization.Core.Application;
using Localization.Core.Configuration;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Localization.Core.Di;

internal sealed class LocalizationModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        Core.Configuration.Configure(builder =>
            builder.AddJsonFile("localization.json", optional: true, reloadOnChange: true));
        services
            .AddOptionsWithValidateOnStart<LocalizationFallbackConfig>()
            .BindConfiguration(string.Empty);
        services.AddSwiftly(Core);

        AddSingleton<LocalizationCache>(services);
        AddSingleton<PlayerLanguageCache>(services);
        AddSingleton<LanguageResolver>(services);
        AddSingleton<ILanguageResolver>(services, provider => provider.GetRequiredService<LanguageResolver>());
        AddSingleton<LocalizationRuntime>(services);
        AddSingleton<LocalizationApi>(services);
        AddSingleton<FallbackLocalizationProvider>(services);
        AddSingleton<DatabaseLocalizationProvider>(services);
        AddSingleton<PlayerLanguagePreferenceRepository>(services);
        AddSingleton<PlayerLanguageSelectionService>(services);
        AddSingleton<RateLimitedLocalizationLogger>(services, _ => new RateLimitedLocalizationLogger(Core.Logger));
        AddSingleton<LocalizationCoordinator>(services, provider => new LocalizationCoordinator(
            provider.GetRequiredService<LocalizationCache>(),
            provider.GetRequiredService<DatabaseLocalizationProvider>(),
            provider.GetRequiredService<FallbackLocalizationProvider>(),
            provider.GetRequiredService<RateLimitedLocalizationLogger>(),
            Core.Logger));

        services.AddPostgreSqlDatabase<LocalizationDbContext>(Core, new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = LocalizationDbContext.SchemaName,
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
