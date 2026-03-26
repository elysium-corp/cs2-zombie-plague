using SupplyBox.Events;

namespace SupplyBox;

public interface ISupplyBoxApi
{
    public IEventSubscriber EventSubscriber { get; }
    
    public static readonly string SharedApiKey = "SupplyBox.Core.ISupplyBoxApi";
}