using Admin.Api;
using Admin.Core.Api;
using Admin.Core.Database;
using Admin.Core.Di;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Database.Migrator;
using Common.Di;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

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
    private readonly Lazy<IPrivilegeRegistry> _privilegeRegistry = GetRequiredServiceLazy<IPrivilegeRegistry>();
    private readonly Lazy<IPrivilegeService> _privilegeService = GetRequiredServiceLazy<IPrivilegeService>();
    private readonly Lazy<DatabaseMigrator<AdminDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<AdminDbContext>>();
    
    protected override void OnStart()
    {
        TryMigrateDatabase();
    }
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var api = new AdminApi(_privilegeRegistry.Value, _privilegeService.Value);

        interfaceManager.AddSharedInterface<IAdminApi, AdminApi>(IAdminApi.SharedApiKey, api);
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