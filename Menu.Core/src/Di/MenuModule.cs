using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Menu.Api.Extensions;
using Menu.Core.Access;
using Menu.Core.Api;
using Menu.Core.Application;
using Menu.Core.Audience;
using Menu.Core.Commands;
using Menu.Core.Configuration;
using Menu.Core.Database;
using Menu.Core.Database.Repositories;
using Menu.Core.Extensions;
using Menu.Core.Providers;
using Menu.Core.Runtime;
using Menu.Core.Storage;
using Menu.Core.Swiftly;
using Menu.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Menu.Api.Enums;
using SwiftlyS2.Shared;

namespace Menu.Core.Di;

internal sealed class MenuModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        AddConfig<MenuCoreConfig>(services, "menu.json", "MenuCoreConfig");
        services.AddOptions<MenuCoreConfig>().Validate(
            IsValidConfiguration,
            "MenuCoreConfig contains an invalid server key, permission, interval, command, or snapshot filename.");
        services.AddSwiftly(core);

        AddSingleton<MenuApi>(services);
        AddSingleton<MenuExtensionRegistry>(services);
        AddSingleton<IMenuExtensionRegistry>(
            services,
            provider => provider.GetRequiredService<MenuExtensionRegistry>()
        );
        AddSingleton<IMenuExtensionDispatcher>(
            services,
            provider => provider.GetRequiredService<MenuExtensionRegistry>()
        );

        AddSingleton<AdminAccessResolver>(services);
        AddSingleton<MenuCapabilityProvider>(services);
        AddSingleton<DatabaseProviderStateSink>(services);
        AddSingleton<IProviderStateSink>(
            services,
            provider => provider.GetRequiredService<DatabaseProviderStateSink>());
        AddSingleton<ProviderRegistry>(services);

        AddSingleton<MenuReleaseValidator>(services);
        AddSingleton<MenuSnapshotCompiler>(services);
        AddSingleton<MenuSnapshotStore>(services);
        AddSingleton<MenuReleaseFileStore>(services);
        AddSingleton<MenuBootstrapLoader>(services);
        AddSingleton<MenuValidationContextFactory>(services);
        AddSingleton<SwiftlyMenuAdapter>(services);
        AddSingleton<MenuAudienceResolver>(services, provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MenuCoreConfig>>();
            return new MenuAudienceResolver(
                core,
                provider.GetRequiredService<AdminAccessResolver>(),
                options.Value.BroadcastPermission);
        });
        AddSingleton<MenuRuntimeService>(services);
        AddSingleton<IMenuCommandTarget>(
            services,
            provider => provider.GetRequiredService<MenuRuntimeService>());
        AddSingleton<MenuCommandRouter>(services);

        AddSingleton<MenuReleaseRepository>(services);
        AddSingleton<ProviderStateRepository>(services);
        AddSingleton<MenuRuntimeStatusRepository>(services);
        AddSingleton<MenuSyncCoordinator>(services);

        var menuOptions = core.Configuration.Manager
            .GetSection("MenuCoreConfig")
            .Get<MenuCoreConfig>() ?? new MenuCoreConfig();
        if (!IsValidConfiguration(menuOptions))
        {
            throw new InvalidOperationException(
                "MenuCoreConfig contains an invalid server key, permission, interval, command, or snapshot filename.");
        }

        services.AddPostgreSqlDatabase<MenuDbContext>(core, new DatabaseOptions
        {
            ConnectionName = menuOptions.DatabaseConnectionName,
            Schema = MenuDbContext.SchemaName,
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

    private static bool IsValidConfiguration(MenuCoreConfig config)
    {
        if (!MenuIdentifier.IsTechnicalKey(config.ServerKey)
            || config.ServerKey.Length > 64
            || config.ServerGroups is null
            || config.ServerGroups.Count > 64
            || config.ServerGroups.Any(group => !MenuIdentifier.IsTechnicalKey(group) || group.Length > 64)
            || config.ServerGroups.Distinct(StringComparer.Ordinal).Count() != config.ServerGroups.Count
            || string.IsNullOrWhiteSpace(config.DatabaseConnectionName)
            || config.DatabaseConnectionName.Length > 128
            || config.DatabaseConnectionName.Any(char.IsControl)
            || config.SyncIntervalSeconds is < 5 or > 3_600
            || config.MaxNavigationDepth is < 4 or > 64
            || !IsSnapshotFileName(config.LastKnownGoodFileName)
            || !IsSnapshotFileName(config.FallbackFileName)
            || string.Equals(config.LastKnownGoodFileName, config.FallbackFileName, StringComparison.Ordinal)
            || !IsLocale(config.DefaultLocale)
            || !MenuIdentifier.IsPermission(config.BroadcastPermission)
            || config.ReservedCommands is null
            || config.ReservedCommands.Count > 64
            || config.ReservedCommands.Any(command => !MenuIdentifier.IsAliasValid(MenuCommandKind.Console, command)))
        {
            return false;
        }

        return config.ReservedCommands
            .Select(MenuIdentifier.CanonicalizeAlias)
            .Distinct(StringComparer.Ordinal)
            .Count() == config.ReservedCommands.Count;
    }

    private static bool IsSnapshotFileName(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 128
        && !value.Any(char.IsControl)
        && !value.Contains('/')
        && !value.Contains('\\')
        && value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && value is not ("." or "..")
        && string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);

    private static bool IsLocale(string? value)
    {
        if (value is null || value.Length is < 2 or > 16 || !char.IsLetter(value[0]) || !char.IsLetter(value[1]))
        {
            return false;
        }

        return value.All(static character =>
            character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '-');
    }
}
