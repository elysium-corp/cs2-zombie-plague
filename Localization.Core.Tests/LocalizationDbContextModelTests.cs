using Localization.Core.Database;
using Localization.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Localization.Core.Tests;

public sealed class LocalizationDbContextModelTests
{
    [Fact]
    public void SettingsSingletonKey_IsExplicitAndHasNoConstantDefault()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new LocalizationDbContext(options);

        var property = context.Model
            .FindEntityType(typeof(LocalizationSettingsEntity))!
            .FindProperty(nameof(LocalizationSettingsEntity.Id))!;

        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Null(property.GetDefaultValue());
    }
}
