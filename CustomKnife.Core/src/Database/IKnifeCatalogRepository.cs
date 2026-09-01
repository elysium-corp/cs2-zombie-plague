using CustomKnife.Data.Models;

namespace CustomKnife.Database;

internal interface IKnifeCatalogRepository
{
    IReadOnlyCollection<IKnife> GetEnabledKnives();
}
