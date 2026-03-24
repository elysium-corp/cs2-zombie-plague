using SwiftlyS2.Shared.Natives;

namespace ZPCore.Data.Plugins.SupplyBox;

internal sealed class MapSupplyBoxEntityConfig
{
    public List<SupplyBoxEntityConfig> SupplyBoxes { get; set; } = [];
}

public sealed class SupplyBoxEntityConfig
{
    public int Index { get; set; }
    public Vector Position { get; set; }
    public Vector Rotation { get; set; }
}