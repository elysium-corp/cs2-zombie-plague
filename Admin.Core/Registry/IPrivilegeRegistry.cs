using Admin.Api.Data;

namespace Admin.Core.Registry;

internal interface IPrivilegeRegistry
{
    IPrivilege Register(PrivilegeDefinition definition);

    IPrivilege? Find(string group, string id);

    IReadOnlyCollection<IPrivilege> GetAll();
}