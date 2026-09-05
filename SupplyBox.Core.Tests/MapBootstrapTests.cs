using SupplyBox.Services;
using Xunit;

namespace SupplyBox.Tests;

public sealed class MapBootstrapTests
{
    [Fact]
    public void ColdStartWithoutGlobalVarsWaitsForMapInsteadOfFailingHostStartup()
    {
        var loadedMaps = new List<string>();

        var initialized = SupplyBoxMapBootstrap.TryLoadCurrentMap(
            () => throw new InvalidOperationException("GlobalVars is null."),
            loadedMaps.Add);

        Assert.False(initialized);
        Assert.Empty(loadedMaps);
    }

    [Theory]
    [InlineData("de_mirage")]
    [InlineData("workshop/123/zm_test")]
    public void PluginReloadLoadsTheExistingMap(string mapName)
    {
        var loadedMaps = new List<string>();

        var initialized = SupplyBoxMapBootstrap.TryLoadCurrentMap(() => mapName, loadedMaps.Add);

        Assert.True(initialized);
        Assert.Equal(mapName, Assert.Single(loadedMaps));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyMapNameDoesNotLoadAnInvalidConfiguration(string? mapName)
    {
        var loadedMaps = new List<string>();

        var initialized = SupplyBoxMapBootstrap.TryLoadCurrentMap(() => mapName, loadedMaps.Add);

        Assert.False(initialized);
        Assert.Empty(loadedMaps);
    }

    [Fact]
    public void ConfigurationFailureIsNotMistakenForMissingGlobalVars()
    {
        var failure = new InvalidOperationException("Configuration service failed.");

        var actual = Assert.Throws<InvalidOperationException>(() =>
            SupplyBoxMapBootstrap.TryLoadCurrentMap(() => "de_mirage", _ => throw failure));

        Assert.Same(failure, actual);
    }

    [Fact]
    public void UnexpectedEngineFailureIsNotSuppressed()
    {
        var failure = new IOException("Engine lookup failed.");

        var actual = Assert.Throws<IOException>(() =>
            SupplyBoxMapBootstrap.TryLoadCurrentMap(() => throw failure, _ => { }));

        Assert.Same(failure, actual);
    }
}
