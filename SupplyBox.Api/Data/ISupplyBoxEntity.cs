using SwiftlyS2.Shared.SchemaDefinitions;

namespace SupplyBox.Data;

public interface ISupplyBoxEntity
{
    public CDynamicProp? Entity { get; }
    
    public int Index { get; }
}