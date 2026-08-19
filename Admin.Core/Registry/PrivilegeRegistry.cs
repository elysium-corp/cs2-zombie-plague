using System.Collections.Frozen;
using Admin.Api.Data;
using Admin.Core.Data;

namespace Admin.Core.Registry;

internal sealed class PrivilegeRegistry : IPrivilegeRegistry
{
    private readonly Dictionary<string, Privilege> _privileges = new(StringComparer.OrdinalIgnoreCase);

    public IPrivilege Register(PrivilegeDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Group);

        var privilege = new Privilege
        {
            Id = definition.Id,
            Group = definition.Group,
            Permissions = definition.Permissions.ToFrozenSet(StringComparer.OrdinalIgnoreCase)
        };

        if (_privileges.TryGetValue(privilege.Key, out var existing))
        {
            if (existing.Permissions.SetEquals(privilege.Permissions))
            {
                return existing;
            }

            throw new InvalidOperationException($"Privilege '{privilege.Key}' is already registered!");
        }

        _privileges.Add(privilege.Key, privilege);

        return privilege;
    }

    public IPrivilege? Find(string group, string id
    )
    {
        return _privileges.GetValueOrDefault(CreateKey(group, id));
    }

    public IReadOnlyCollection<IPrivilege> GetAll()
    {
        return _privileges.Values;
    }

    private static string CreateKey(string group, string id)
    {
        return $"{group}.{id}";
    }
}