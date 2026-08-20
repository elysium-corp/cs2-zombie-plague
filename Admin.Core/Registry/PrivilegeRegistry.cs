using System.Collections.Frozen;
using Admin.Api.Data;
using Admin.Core.Data;

namespace Admin.Core.Registry;

internal sealed class PrivilegeRegistry : IPrivilegeRegistry
{
    private static readonly FrozenDictionary<string, Privilege> Empty = new Dictionary<string, Privilege>()
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private volatile FrozenDictionary<string, Privilege> _privileges = Empty;

    public void ReplaceAll(IEnumerable<PrivilegeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var privileges = new Dictionary<string, Privilege>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var definition in definitions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(definition.Group);

            var privilege = new Privilege
            {
                Id = definition.Id,
                Group = definition.Group,
                Permissions = definition.Permissions.ToFrozenSet(
                    StringComparer.OrdinalIgnoreCase
                )
            };

            if (!privileges.TryAdd(privilege.Key, privilege))
            {
                throw new InvalidOperationException($"Privilege '{privilege.Key}' is duplicated!");
            }
        }

        _privileges = privileges.ToFrozenDictionary(
            StringComparer.OrdinalIgnoreCase
        );
    }

    public IPrivilege? Find(string key)
    {
        return _privileges.GetValueOrDefault(key);
    }

    public IReadOnlyCollection<IPrivilege> GetAll()
    {
        return _privileges.Values;
    }
}