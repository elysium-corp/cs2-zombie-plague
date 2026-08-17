using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;

namespace MovementUnlocker;

[PluginMetadata(
    Id = "MovementUnlocker",
    Version = "0.1.0",
    Name = "MovementUnlocker",
    Author = "illusion",
    Description = "Unlock movement"
)]
public partial class MovementUnlocker(ISwiftlyCore core) : BasePlugin(core)
{
    public override void Load(bool hotReload)
    {
        if (!Core.GameData.HasSignature("MovementUnlocker"))
        {
            throw new Exception("MovementUnlocker signature not found");
        }

        Core.GameData.ApplyPatch("MovementUnlocker");
    }

    public override void Unload()
    {
    }
}