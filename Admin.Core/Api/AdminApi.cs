using Admin.Api;
using Admin.Api.Data;
using Admin.Core.Registry;

namespace Admin.Core.Api;

internal sealed class AdminApi(IPrivilegeRegistry privilegeRegistry) : IAdminApi
{
    public IPrivilege RegisterPrivilege(PrivilegeDefinition definition)
    {
        return privilegeRegistry.Register(definition);
    }

    public IPrivilege? FindPrivilege(string group, string id)
    {
        return privilegeRegistry.Find(group, id);
    }

    public IReadOnlyCollection<IPrivilege> GetPrivileges()
    {
        return privilegeRegistry.GetAll();
    }
}