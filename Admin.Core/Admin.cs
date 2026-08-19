using Admin.Api;
using Admin.Core.Api;
using Admin.Core.Database;
using Admin.Core.Di;
using Admin.Core.Managers;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Database.Migrator;
using Common.Di;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
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
    private readonly Lazy<PlayerPrivilegeManager> _playerPrivilegeManager = GetRequiredServiceLazy<PlayerPrivilegeManager>();
    
    protected override void OnStart()
    {
        TryMigrateDatabase();
    }
    
    protected override void OnReady()
    {
        _guidOnPlayerConnectFullPost = Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnPlayerDisconnectPre = Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
    }
    
    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        Core.GameEvent.Unhook(_guidOnPlayerDisconnectPre);

        _playerPrivilegeManager.Value.StopAndWait();
    }
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var api = new AdminApi(_privilegeRegistry.Value, _privilegeService.Value);

        interfaceManager.AddSharedInterface<IAdminApi, AdminApi>(IAdminApi.SharedApiKey, api);
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
    
    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(exception, "Admin database migration failed. Database privileges will be unavailable!");
        }
    }
}