using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;

namespace SupplyBox.Services;

internal sealed class SupplyBoxEditService(SupplyBoxMapConfigService mapConfigService)
{
    public Task<bool> AddSupplyBoxEntity(SupplyBoxEntityTemplate data) => mapConfigService.AddAsync(data.Position, data.Rotation);
    public Task<bool> RemoveSupplyBoxEntity(SupplyBoxEntityConfig data) => mapConfigService.RemoveAsync(data.Index);
}
