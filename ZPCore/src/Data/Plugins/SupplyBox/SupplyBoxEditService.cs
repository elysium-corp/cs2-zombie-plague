using ZPCore.Utils;

namespace ZPCore.Data.Plugins.SupplyBox;

internal sealed class SupplyBoxEditService(SupplyBoxMapConfigService mapConfigService)
{
    public void AddSupplyBoxEntity(SupplyBoxEntityTemplate data)
    {
        if (mapConfigService.MapConfig == null || mapConfigService.SupplyBoxesData == null)
        {
            return;
        }
        
        var nextIndex = mapConfigService.SupplyBoxesData.Count == 0 ? 1 : mapConfigService.SupplyBoxesData.Max(x => x.Index) + 1;
        
        var supplyBoxEntityConfig = new SupplyBoxEntityConfig
        {
            Index = nextIndex,
            Position = data.Position,
            Rotation = data.Rotation
        };
        
        mapConfigService.SupplyBoxesData.Add(supplyBoxEntityConfig);
        
        mapConfigService.SaveConfig();
    }
    
    public void RemoveSupplyBoxEntity(SupplyBoxEntityConfig data)
    {
        if (mapConfigService.MapConfig == null || mapConfigService.SupplyBoxesData == null)
        {
            return;
        }
        
        mapConfigService.SupplyBoxesData.Remove(data);
        
        mapConfigService.SaveConfig();
    }
    
    public SupplyBoxEntity? TrySpawnSupplyBox()
    {
        if (mapConfigService.SupplyBoxesData == null || mapConfigService.SupplyBoxesData.Count == 0)
        {
            return null;
        }
        
        var supplyBoxEntity = new SupplyBoxEntity(mapConfigService.SupplyBoxesData[Numeric.Random(0, mapConfigService.SupplyBoxesData.Count)]);
        supplyBoxEntity.Spawn();
        
        return supplyBoxEntity;
    }
    
    public SupplyBoxEntity? TrySpawnUniqueSupplyBox(List<SupplyBoxEntity> droppedSupplyBoxes)
    {
        if (mapConfigService.SupplyBoxesData == null)
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
        
        var supplyBoxesDataSnapshot = mapConfigService.SupplyBoxesData.Shuffle().ToList();
        var data = supplyBoxesDataSnapshot.Find(box => !spawnedSupplyBoxIndex.Contains(box.Index));
        var supplyBoxEntity = data == null ? new SupplyBoxEntity(supplyBoxesDataSnapshot.First()) : new SupplyBoxEntity(data);
        
        supplyBoxEntity.Spawn();
        
        return supplyBoxEntity;
    }
}