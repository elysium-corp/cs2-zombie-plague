using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;

namespace CustomKnife.Initializer;

internal sealed class KnifeRegistryInitializer(IEnumerable<IKnife> knives, IKnivesRegistry registry )
{
    public void Initialize()
    {
        foreach (var knife in knives)
        {
            if (!knife.Enabled)
            {
                continue;
            }

            if (!registry.TryRegister(knife))
            {
                throw new InvalidOperationException($"Knife '{knife.InternalName}' is already registered!");
            }
        }
    }
}