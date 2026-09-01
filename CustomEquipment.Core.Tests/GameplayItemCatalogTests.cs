using CustomEquipment.Data.GameplayItems;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class GameplayItemCatalogTests
{
    [Fact]
    public void Replace_PublishesCompleteSnapshot()
    {
        var catalog = new GameplayItemCatalog();
        var replacement = GameplayItemDefaults.All
            .Select(definition => definition.ImplementationKey == GameplayItemKeys.FireNade
                ? definition with { DisplayName = "Updated Fire Nade", Enabled = false }
                : definition)
            .ToArray();

        catalog.Replace(replacement);

        var fireNade = catalog.Get(GameplayItemKeys.FireNade);
        Assert.Equal("Updated Fire Nade", fireNade.DisplayName);
        Assert.False(fireNade.Enabled);
    }

    [Fact]
    public void Replace_RejectsIncompleteSnapshotWithoutChangingCurrentValues()
    {
        var catalog = new GameplayItemCatalog();
        var original = catalog.Get(GameplayItemKeys.LaserMine);
        var incomplete = GameplayItemDefaults.All
            .Where(definition => definition.ImplementationKey != GameplayItemKeys.LaserMine)
            .ToArray();

        Assert.Throws<InvalidOperationException>(() => catalog.Replace(incomplete));
        Assert.Same(original, catalog.Get(GameplayItemKeys.LaserMine));
    }
}
