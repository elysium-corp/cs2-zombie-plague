using Admin.Api;
using Economy.Api;
using Admin.Core.Api;
using Admin.Core.Database;
using Admin.Core.Di;
using Admin.Core.Managers;
using Admin.Core.Menus;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Database.Migrator;
using Common.Di;
using Common.Di.Diagnostics;
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
    Version = "0.3.0",
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
    
    private readonly Lazy<AdminMoneyService> _money = GetRequiredServiceLazy<AdminMoneyService>();
    private readonly Lazy<AdminMovementService> _movement = GetRequiredServiceLazy<AdminMovementService>();
    private readonly Lazy<CommunicationService> _communication = GetRequiredServiceLazy<CommunicationService>();
    private readonly Lazy<AdminPlayerActionService> _actions = GetRequiredServiceLazy<AdminPlayerActionService>();
    private readonly Lazy<AdminActionCommands> _actionCommands = GetRequiredServiceLazy<AdminActionCommands>();

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

        _movement.Value.Start();
        _communication.Value.Start();
        _actions.Value.Start();
        _actionCommands.Value.Start();
        _adminMenu.Value.RegisterCommands();
        _playerPrivilegeRefreshService.Value.Start();
    }
    
    protected override void OnUnload()
    {
        Core.Event.OnClientSteamAuthorize -= OnClientSteamAuthorize;

        _actions.Value.Stop();
        _actionCommands.Value.Dispose();
        _movement.Value.Dispose();
        _communication.Value.Dispose();
        _money.Value.Economy = null;
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
        try
        {
            _money.Value.Economy = interfaceManager.GetSharedInterface<IEconomyApi>(IEconomyApi.SharedApiKey);
        }
        catch (Exception exception)
        {
            _money.Value.Economy = null;
            Core.Logger.LogWarning(exception, "Economy API is unavailable; admin money grants are disabled");
        }
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
        using var timing = ConnectionDiagnostics.Begin(Core.Logger, "Admin.steam_authorize", @event.PlayerId);
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        timing?.Identify(player);

        if (player is null)
        {
            return;
        }

        _banEnforcementService.Value.Check(player);
    }
    
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        using var timing = ConnectionDiagnostics.Begin(Core.Logger, "Admin.player_connect_full");
        var player = @event.UserIdPlayer;
        timing?.Identify(player);

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
