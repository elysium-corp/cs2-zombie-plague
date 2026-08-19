using Admin.Api.Data;

namespace Admin.Api;

public interface IAdminApi
{
    IPrivilege RegisterPrivilege(PrivilegeDefinition definition);

    IPrivilege? FindPrivilege(string group, string id);

    IReadOnlyCollection<IPrivilege> GetPrivileges();

    public static readonly string SharedApiKey = "Admin.Api.IAdminApi";
}