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
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Localization.Core.Di;

internal sealed class LocalizationModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        Core.Configuration.Configure(builder =>
            builder.AddJsonFile(source =>
            {
                source.Path = "localization.json";
                source.Optional = true;
                source.ReloadOnChange = true;
                source.OnLoadException = context =>
                {
                    Core.Logger.LogError(
                        context.Exception,
                        "[Localization] localization.json is invalid and was ignored. " +
                        "The current memory snapshot remains active until a source is loaded."
                    );
                    context.Ignore = true;
                };
            }));
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
            provider.GetRequiredService<RateLimitedLocalizationLogger>()));

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
