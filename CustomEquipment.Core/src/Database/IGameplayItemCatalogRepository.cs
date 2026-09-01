using CustomEquipment.Data.GameplayItems;

namespace CustomEquipment.Database;

internal interface IGameplayItemCatalogRepository
{
    IReadOnlyCollection<GameplayItemDefinition> GetItems();
}
