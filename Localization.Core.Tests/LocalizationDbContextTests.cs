using Localization.Core.Database;
using Localization.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Localization.Core.Tests;

public sealed class LocalizationDbContextTests
{
    [Fact]
    public void SettingsId_IsExplicitAndHasNoConstantDatabaseDefault()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=localization_model_test;Username=test;Password=test")
            .Options;

        using var context = new LocalizationDbContext(options);
        var property = context.Model
            .FindEntityType(typeof(LocalizationSettingsEntity))!
            .FindProperty(nameof(LocalizationSettingsEntity.Id))!;

        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Null(property.FindAnnotation(RelationalAnnotationNames.DefaultValue));
    }
}
