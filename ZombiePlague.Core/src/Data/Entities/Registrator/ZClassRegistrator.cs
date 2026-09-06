using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Zombie;

namespace ZombiePlague.Core.Data.Entities.Registrator;

internal sealed class ZClassRegistrator(ZombieCatalogService catalog) : IZClassRegistrator
{
    public IEnumerable<IZClassConfig> GetAll() => catalog.Current.Document.Classes
        .OrderBy(item => item.SortOrder).ThenBy(item => item.InternalName, StringComparer.Ordinal).ToArray();

    public IEnumerable<IZClassConfig> GetAllEnabled() => GetAll().Where(item => item.Enabled).ToArray();

    public void Register() { }
}
