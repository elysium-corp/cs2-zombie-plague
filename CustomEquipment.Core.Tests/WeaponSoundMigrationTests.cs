using CustomEquipment.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponSoundMigrationTests
{
    [Fact]
    public void VariantMigrationAndRuntimeModelStayInSync()
    {
        var options = new DbContextOptionsBuilder<CustomEquipmentDbContext>()
            .UseNpgsql("Host=localhost;Database=custom_equipment_migration_test;Username=test;Password=test")
            .Options;
        using var context = new CustomEquipmentDbContext(options);

        Assert.Contains("20260905120000_AllowWeaponSoundVariants", context.Database.GetMigrations());
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
