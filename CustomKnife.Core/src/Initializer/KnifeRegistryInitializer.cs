using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;

namespace CustomKnife.Initializer;

internal sealed class KnifeRegistryInitializer(IEnumerable<IKnife> knives, IWritableKnivesRegistry registry)
{
    public void Initialize()
    {
        registry.ReplaceAll(knives.Where(knife => knife.Enabled).ToArray());
    }
}
