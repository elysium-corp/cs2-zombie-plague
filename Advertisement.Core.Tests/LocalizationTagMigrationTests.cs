using Advertisement.Core.Configuration;
using Advertisement.Core.Data;
using Advertisement.Core.Database;
using Advertisement.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Advertisement.Core.Tests;

public sealed class LocalizationTagMigrationTests
{
    private const string PreviousMigration = "20260831030000_ReferenceLocalizationKeys";
    private const string FeatureMigration = "20260904121000_ReferenceLocalizationTags";

    [Fact]
    public void UpgradeScript_KeepsOnlyTagKeyReferenceInAdvertisementModel()
    {
        var options = new DbContextOptionsBuilder<AdvertisementDbContext>()
            .UseNpgsql("Host=localhost;Database=advertisement_migration_test;Username=test;Password=test")
            .Options;
        using var context = new AdvertisementDbContext(options);

        var script = context.GetService<IMigrator>().GenerateScript(PreviousMigration, FeatureMigration);

        Assert.Contains("ADD COLUMN IF NOT EXISTS tag_key", script);
        Assert.Contains("REFERENCES localization.tags(key)", script);
        Assert.Contains("DROP TABLE IF EXISTS advertisement.tag_translations", script);
        Assert.Contains("DROP TABLE IF EXISTS advertisement.tags", script);
        Assert.Contains("DROP COLUMN IF EXISTS tag_id", script);
    }

    [Fact]
    public void RuntimeModel_DoesNotOwnTagDefinitionsOrColorPalette()
    {
        Assert.Null(typeof(AdvertisementConfig).GetProperty("Tags"));
        Assert.Null(typeof(AdvertisementConfig).GetProperty("Colors"));
        Assert.Null(typeof(AdvertisementSnapshot).GetProperty("Tags"));

        var options = new DbContextOptionsBuilder<AdvertisementDbContext>()
            .UseNpgsql("Host=localhost;Database=advertisement_model_test;Username=test;Password=test")
            .Options;
        using var context = new AdvertisementDbContext(options);

        var message = context.Model.FindEntityType(typeof(AdvertisementMessageEntity))!;
        Assert.NotNull(message.FindProperty(nameof(AdvertisementMessageEntity.TagKey)));
        Assert.Null(message.FindProperty("TagId"));
        Assert.DoesNotContain(
            context.Model.GetEntityTypes(),
            entity => entity.ClrType.Name.Contains("AdvertisementTag", StringComparison.Ordinal));
    }
}
