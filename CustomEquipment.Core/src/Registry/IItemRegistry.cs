using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Registry;

internal interface IItemRegistry
{
    void Initialize();

    IReadOnlyCollection<IItem> GetDefinitions();

    bool TryGetDefinition(
        string internalName,
        [NotNullWhen(true)] out IItem? definition
    );

    TItem Create<TItem>() where TItem : class, IItem;

    IItem Create(string internalName);
}