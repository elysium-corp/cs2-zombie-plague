using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Database;
using CustomKnife.Database.Entities;
using CustomKnife.Hud;
using CustomKnife.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ZombiePlague.Api.Data;

namespace CustomKnife.Core.Tests;

public sealed class KnifeCmsIntegrationTests
{
    private const string Icon = "panorama/images/custom_game/elysium/equipment/karambit.vsvg";
    private const string Preview = "panorama/images/custom_game/elysium/knife_previews/test_png.vtex";
    private static KnifeDefinition Knife => new(true, "karambit", "Karambit", "Knife.Karambit.Name",
        "weapons/knife.vmdl", "Description", "Knife.Karambit.Description", 250, new KnockbackData(250, 150), 800, 1, null);

    [Fact]
    public void DatabaseMappingIncludesBothAssetsAndRejectsWrongFormats()
    {
        var entity = new KnifeEntity { InternalName = "karambit", DisplayName = "Karambit",
            DisplayNameKey = "Knife.Karambit.Name", Description = "Description", DescriptionKey = "Knife.Karambit.Description",
            Model = "weapons/knife.vmdl", Speed = 250, Gravity = 800, DamageMultiplier = 1,
            HudIconPath = Icon, HudPreviewPath = Preview };
        var knife = Assert.IsType<KnifeDefinition>(KnifeCatalogRepository.Map(entity));
        Assert.Equal(Icon, knife.HudIconPath);
        Assert.Equal(Preview, knife.HudPreviewPath);
        entity.HudIconPath = Preview;
        Assert.Throws<InvalidOperationException>(() => KnifeCatalogRepository.Map(entity));
    }

    [Theory]
    [InlineData("https://example.com/knife.png", true)]
    [InlineData("panorama/images/../knife.vsvg", false)]
    [InlineData("panorama/images/knife.vsvg_c", false)]
    [InlineData("panorama/images/knife_png.vtex\n", true)]
    [InlineData("panorama/images/knife.png", true)]
    [InlineData("panorama/images/knife\".vsvg", false)]
    public void PathsCannotInjectCssOrUseAnUncompiledWebAsset(string path, bool preview)
        => Assert.Throws<InvalidOperationException>(() => KnifeHudAssets.Validate(path, preview));

    [Fact]
    public void RuntimeModelAndMigrationSnapshotAgree()
    {
        using var context = new CustomKnifeDbContext(new DbContextOptionsBuilder<CustomKnifeDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=metadata;Password=metadata").Options);
        Assert.False(context.Database.HasPendingModelChanges());
        var script = context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript(
            "20260904153000_NormalizeLocalizationReferences", "20260920120000_AddKnifeHudAssets");
        Assert.Contains("hud_icon_path", script);
        Assert.Contains("hud_preview_path", script);
    }

    [Fact]
    public async Task PollingPreservesIdentityUntilDataChangesAndKeepsSnapshotOnFailure()
    {
        var repository = new Mock<IKnifeCatalogRepository>();
        var registry = new KnivesRegistry();
        var first = Knife;
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first]);
        using var sync = new KnifeCatalogSynchronizer(repository.Object, registry, Options.Create(new KnifeHudOptions()),
            NullLogger<KnifeCatalogSynchronizer>.Instance);
        Assert.True(await sync.ReloadAsync(default));
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Knife]);
        Assert.True(await sync.ReloadAsync(default));
        Assert.Same(first, registry.GetAll().Single());
        Assert.Equal(1, sync.Revision);
        var updated = Knife with { Description = "Updated", Speed = 320, HudIconPath = Icon };
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([updated]);
        Assert.True(await sync.ReloadAsync(default));
        Assert.Same(updated, registry.GetAll().Single());
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("offline"));
        Assert.False(await sync.ReloadAsync(default));
        Assert.Same(updated, registry.GetAll().Single());
        Assert.Equal(2, sync.Revision);
    }

    [Fact]
    public async Task CancelledQueryCannotPublishAndNextReloadStillWorks()
    {
        var repository = new Mock<IKnifeCatalogRepository>();
        var registry = new KnivesRegistry();
        using var cancellation = new CancellationTokenSource();
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => { cancellation.Cancel(); return Task.FromResult<IReadOnlyCollection<IKnife>>([Knife]); });
        using var sync = new KnifeCatalogSynchronizer(repository.Object, registry, Options.Create(new KnifeHudOptions()),
            NullLogger<KnifeCatalogSynchronizer>.Instance);
        await Assert.ThrowsAsync<OperationCanceledException>(() => sync.ReloadAsync(cancellation.Token));
        Assert.Empty(registry.GetAll());
        repository.Setup(r => r.GetEnabledKnivesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Knife]);
        Assert.True(await sync.ReloadAsync(default));
        Assert.Single(registry.GetAll());
    }
}
