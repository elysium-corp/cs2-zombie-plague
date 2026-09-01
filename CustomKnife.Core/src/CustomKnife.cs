using Admin.Api;
using Common.Database.Migrator;
using Common.Database.Tasks;
using Common.Di;
using CustomKnife.Data.Menus;
using CustomKnife.Database;
using CustomKnife.Di;
using CustomKnife.Initializer;
using CustomKnife.Services;
using Menu.Api;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using ZombiePlague.Api;

namespace CustomKnife;

[PluginMetadata(
    Id = "CustomKnife.Core",
    Version = "0.2.0",
    Name = "[ZP] CustomKnife",
    Author = "illusion & fdrinv",
    Description = "Database-backed custom knives with Admin.Core permissions"
)]
internal sealed partial class CustomKnife(ISwiftlyCore core) : Plugin<CustomKnifeModule>(core)
{
    private readonly Lazy<DatabaseMigrator<CustomKnifeDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<CustomKnifeDbContext>>();
    private readonly Lazy<DatabaseTaskTracker> _databaseTaskTracker = GetRequiredServiceLazy<DatabaseTaskTracker>();
    private readonly Lazy<CustomKnifeCoordinator> _coordinator = GetRequiredServiceLazy<CustomKnifeCoordinator>();
    private readonly Lazy<MenuApiBridge> _menuApiBridge = GetRequiredServiceLazy<MenuApiBridge>();
    private readonly Lazy<KnifeRegistryInitializer> _knifeRegistryInitializer = GetRequiredServiceLazy<KnifeRegistryInitializer>();
    private readonly Lazy<KnifeCatalogSynchronizer> _catalogSynchronizer = GetRequiredServiceLazy<KnifeCatalogSynchronizer>();
    private readonly Lazy<AdminApiProxy> _adminApiProxy = GetRequiredServiceLazy<AdminApiProxy>();
    private readonly Lazy<KnifeAccessMonitor> _knifeAccessMonitor = GetRequiredServiceLazy<KnifeAccessMonitor>();
    private Guid _reloadCommand = Guid.Empty;
    private bool _isReady;
    
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);
        _menuApiBridge.Value.Initialize(menuApi);

        if (interfaceManager.TryGetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey, out var adminApi))
        {
            _adminApiProxy.Value.Initialize(adminApi);
        }
        else
        {
            Core.Logger.LogWarning(
                "[CustomKnife] Admin.Core не загружен. Ножи с required_permission будут недоступны."
            );
        }
    }

    protected override void OnStart()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "CustomKnife database migration failed. Compiled fallback knives remain available."
            );
        }
    }

    protected override void OnReady()
    {
        _isReady = true;
        _knifeRegistryInitializer.Value.Initialize();
        _catalogSynchronizer.Value.TryReload(out _);
        Core.Event.OnMapLoad += OnMapLoad;
        _knifeAccessMonitor.Value.Tick();
        _reloadCommand = Core.Command.RegisterCommand(
            commandName: "custom_knife_reload",
            handler: ReloadHandler,
            registerRaw: true,
            helpText: "Reload CustomKnife catalog from PostgreSQL"
        );
        _coordinator.Value.Start();
    }

    protected override void OnUnload()
    {
        _isReady = false;

        if (_reloadCommand != Guid.Empty)
        {
            Core.Command.UnregisterCommand(_reloadCommand);
            _reloadCommand = Guid.Empty;
        }

        Core.Event.OnMapLoad -= OnMapLoad;
        _adminApiProxy.Value.Uninitialize();
        _coordinator.Value.Stop();
        
        _databaseTaskTracker.Value.StopAndWait();
    }

    private void ReloadHandler(ICommandContext context)
    {
        if (context.IsSentByPlayer)
        {
            context.Reply("This command can only be executed from the server console.");
            return;
        }

        if (_catalogSynchronizer.Value.TryReload(out var count))
        {
            context.Reply($"CustomKnife reloaded: {count} enabled knives.");
            return;
        }

        context.Reply("CustomKnife reload failed; the previous snapshot is still active.");
    }

    private void OnMapLoad(IOnMapLoadEvent mapLoadEvent)
    {
        _ = mapLoadEvent;
        Core.Scheduler.NextWorldUpdate(() =>
        {
            if (_isReady)
            {
                _catalogSynchronizer.Value.TryReload(out _);
                _knifeAccessMonitor.Value.Tick();
            }
        });
    }
}
