using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Registration;
using CustomEquipment.Data.DatabaseWeapons;

namespace CustomEquipment.Registry;

internal interface IItemRegistry : IEquipmentRegistrar
{
    void Initialize();

    void ReplaceDatabaseWeapons(IReadOnlyCollection<DatabaseWeaponItem> weapons);

    IReadOnlyCollection<IItem> GetDefinitions();

    bool TryGetDefinition(
        string internalName,
        [NotNullWhen(true)] out IItem? definition
    );

    TItem Create<TItem>() where TItem : class, IItem;

    IItem Create(string internalName);
}
