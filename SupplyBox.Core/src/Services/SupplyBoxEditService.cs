using Common.Di;
using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;
using SupplyBox.Utils;

namespace SupplyBox.Services;

internal sealed class SupplyBoxEditService(SupplyBoxMapConfigService mapConfigService)
{
    public void AddSupplyBoxEntity(SupplyBoxEntityTemplate data)
    {
        mapConfigService.TryAdd(data.Position, data.Rotation);
    }
    
    public void RemoveSupplyBoxEntity(SupplyBoxEntityConfig data)
    {
        mapConfigService.TryRemove(data.Index);
    }
    
    public SupplyBoxEntity? TrySpawnSupplyBox()
    {
        var points = mapConfigService.GetSnapshot();
        if (points.Count == 0)
        {
            return null;
        }
        
        var supplyBoxData = points[Numeric.Random(0, points.Count)];
        
        var supplyBoxEntity = DependencyResolver.GetRequiredService<SupplyBoxEntity>();
        supplyBoxEntity.Spawn(supplyBoxData);
        
        return supplyBoxEntity;
    }
    
    public SupplyBoxEntity? TrySpawnUniqueSupplyBox(List<SupplyBoxEntity> droppedSupplyBoxes)
    {
        var points = mapConfigService.GetSnapshot();
        if (points.Count == 0)
        {
            return null;
        }
        
        if (droppedSupplyBoxes.Count == 0)
        {
            return TrySpawnSupplyBox();
        }

        List<int> spawnedSupplyBoxIndex = [];
        foreach (var box in droppedSupplyBoxes)
        {
            spawnedSupplyBoxIndex.Add(box.Index);
        }
        
        var supplyBoxesDataSnapshot = points.Shuffle().ToList();
        var data = supplyBoxesDataSnapshot.Find(box => !spawnedSupplyBoxIndex.Contains(box.Index));
        var newData = data == null ? supplyBoxesDataSnapshot.First() : data;
        
        var supplyBoxEntity = DependencyResolver.GetRequiredService<SupplyBoxEntity>();
        supplyBoxEntity.Spawn(newData);
        
        return supplyBoxEntity;
    }
}
