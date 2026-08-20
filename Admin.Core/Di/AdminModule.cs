using Admin.Core.Database;
using Admin.Core.Managers;
using Admin.Core.Registry;
using Admin.Core.Services;
using Admin.Core.Store;
using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Admin.Core.Di;

internal sealed class AdminModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(Core);
        
        BuildSingletons(service);
        AddDatabase(service);

        return (service.BuildServiceProvider(), service);
    }
    
    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<IPrivilegeRegistry, PrivilegeRegistry>(service);
        AddSingleton<IPlayerPrivilegeStore, PlayerPrivilegeStore>(service);
        AddSingleton<IPrivilegeService, PrivilegeService>(service);
        AddSingleton<IPlayerPrivilegeManager, PlayerPrivilegeManager>(service);
        AddSingleton<IPlayerPrivilegePersistenceService, PlayerPrivilegePersistenceService>(service);
        AddSingleton<IPlayerPrivilegeRefreshService, PlayerPrivilegeRefreshService>(service);
    }

    private void AddDatabase(ServiceCollection service)
    {
        var options = new DatabaseOptions
        {
            ConnectionName = "elysium_zp_server_1",
            Schema = AdminDbContext.SchemaName,
            CommandTimeoutSeconds = 5,
            RetryCount = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(3)
        };

        service.AddPostgreSqlDatabase<AdminDbContext>(Core, options);
    }
}