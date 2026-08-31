using Admin.Api;
using Admin.Core.Api;
using Admin.Core.Database;
using Admin.Core.Di;
using Admin.Core.Managers;
using Admin.Core.Menus;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Database.Migrator;
using Common.Di;
using Localization.Api;
using Menu.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Admin.Core;

[PluginMetadata(
    Id = "Admin.Core", 
    Version = "0.1.0", 
    Name = "Admin Core", 
    Author = "illusion & fdrinv",
    Description = "Added privileges"
)]
internal sealed partial class Admin(ISwiftlyCore core) : Plugin<AdminModule>(core)
{
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnPlayerDisconnectPre = Guid.Empty;
    
    private readonly Lazy<IPrivilegeRegistry> _privilegeRegistry = GetRequiredServiceLazy<IPrivilegeRegistry>();
    private readonly Lazy<IPrivilegeService> _privilegeService = GetRequiredServiceLazy<IPrivilegeService>();
    private readonly Lazy<DatabaseMigrator<AdminDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<AdminDbContext>>();
    private readonly Lazy<IPlayerPrivilegeManager> _playerPrivilegeManager = GetRequiredServiceLazy<IPlayerPrivilegeManager>();
    private readonly Lazy<IPlayerPrivilegeRefreshService> _playerPrivilegeRefreshService = GetRequiredServiceLazy<IPlayerPrivilegeRefreshService>();
    private readonly Lazy<IPrivilegeCatalogService> _privilegeCatalogService = GetRequiredServiceLazy<IPrivilegeCatalogService>();
    
    private readonly Lazy<IBanEnforcementService> _banEnforcementService = GetRequiredServiceLazy<IBanEnforcementService>();
    
    private readonly Lazy<AdminMenu> _adminMenu = GetRequiredServiceLazy<AdminMenu>();
    private readonly Lazy<MenuExtensionDispatcherProxy> _menuApiBridge = GetRequiredServiceLazy<MenuExtensionDispatcherProxy>();
    
    protected override void OnStart()
    {
        if (!TryMigrateDatabase())
        {
            return;
        }

        TryLoadPrivilegeCatalog();
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }
    
    protected override void OnReady()
    {
        Core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;

        _guidOnPlayerConnectFullPost = Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnPlayerDisconnectPre = Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);

        _adminMenu.Value.RegisterCommands();
        _playerPrivilegeRefreshService.Value.Start();
    }
    
    protected override void OnUnload()
    {
        Core.Event.OnClientSteamAuthorize -= OnClientSteamAuthorize;

        _adminMenu.Value.UnregisterCommands();

        Core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        Core.GameEvent.Unhook(_guidOnPlayerDisconnectPre);

        _playerPrivilegeRefreshService.Value.StopAndWait();
        _playerPrivilegeManager.Value.StopAndWait();
    }
    
    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);

        _menuApiBridge.Value.Initialize(menuApi);
    }
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var api = new AdminApi(
            _privilegeRegistry.Value,
            _privilegeService.Value
        );

        interfaceManager.AddSharedInterface<IAdminApi, AdminApi>(IAdminApi.SharedApiKey, api);
    }
    
    private void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);

        if (player is null)
        {
            return;
        }

        _banEnforcementService.Value.Check(player);
    }
    
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return HookResult.Continue;
        }

        _playerPrivilegeManager.Value.Initialize(player);

        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        _playerPrivilegeManager.Value.Remove(player);

        return HookResult.Continue;
    }
    
    private bool TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();

            return true;
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Admin database migration failed. Database privileges will be unavailable!"
            );

            return false;
        }
    }
    
    private void TryLoadPrivilegeCatalog()
    {
        try
        {
            _privilegeCatalogService.Value
                .ReloadAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Failed to load admin privilege catalog!"
            );
        }
    }
}
