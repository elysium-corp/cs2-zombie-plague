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
        Assert.Null(property.FindAnnotation(RelationalAnnotationNames.DefaultValue));
        Assert.Null(property.GetDefaultValueSql());
    }

    [Fact]
    public void EntryParameters_AreStoredAsJsonArray()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new LocalizationDbContext(options);

        var property = context.Model
            .FindEntityType(typeof(LocalizationEntryEntity))!
            .FindProperty(nameof(LocalizationEntryEntity.ParametersJson))!;

        Assert.Equal("jsonb", property.GetColumnType());
        Assert.Equal("'[]'::jsonb", property.GetDefaultValueSql());
    }

    [Fact]
    public void SettingsColorTags_AreStoredAsJsonObject()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new LocalizationDbContext(options);

        var property = context.Model
            .FindEntityType(typeof(LocalizationSettingsEntity))!
            .FindProperty(nameof(LocalizationSettingsEntity.ColorTagsJson))!;

        Assert.Equal("jsonb", property.GetColumnType());
        Assert.Contains("\"success\":\"green\"", property.GetDefaultValueSql() ?? string.Empty);
    }

    [Fact]
    public void Settings_TrackLastExportedFallbackVersion()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new LocalizationDbContext(options);

        var property = context.Model
            .FindEntityType(typeof(LocalizationSettingsEntity))!
            .FindProperty(nameof(LocalizationSettingsEntity.FallbackExportedVersion))!;

        Assert.Equal(0L, Assert.IsType<long>(property.GetDefaultValue()));
    }

    [Fact]
    public void LocalizationTag_ReferencesAnEntryByLocalizationKey()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata")
            .Options;
        using var context = new LocalizationDbContext(options);

        var entity = context.Model.FindEntityType(typeof(LocalizationTagEntity))!;
        var foreignKey = Assert.Single(entity.GetForeignKeys());

        Assert.Equal(
            nameof(LocalizationTagEntity.LocalizationKey),
            Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(
            nameof(LocalizationEntryEntity.Key),
            Assert.Single(foreignKey.PrincipalKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
}
