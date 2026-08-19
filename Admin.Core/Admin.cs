using Admin.Api;
using Admin.Core.Api;
using Admin.Core.Di;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Di;
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
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var api = new AdminApi(_privilegeRegistry.Value, _privilegeService.Value);

        interfaceManager.AddSharedInterface<IAdminApi, AdminApi>(IAdminApi.SharedApiKey, api);
    }
}