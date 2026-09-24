using MapRotation.Api;
using MapRotation.Core.Domain;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class CatalogDiagnosticsTests
{
    [Fact]
    public void CatalogExplainsCurrentDisabledUninstalledAndRestrictedMaps()
    {
        var f = new RotationEngineTests.Fixture();
        var maps = f.Maps.Select(map => map.Id switch
        {
            3 => map with { Enabled = false },
            4 => map with { AllowNomination = false, AllowVote = false, AllowAutoRotation = false },
            _ => map
        });
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, maps), [1, 3, 4]);
        var catalog = f.Engine.Catalog().ToDictionary(map => map.Id);

        Assert.Equal("CurrentMap", catalog[1].PoolExclusion);
        Assert.Equal("EngineRejected", catalog[2].PoolExclusion);
        Assert.Equal("Disabled", catalog[3].PoolExclusion);
        Assert.Equal("NoVotingOrRotation", catalog[4].PoolExclusion);
        Assert.Equal("NominationDisabled", catalog[4].NominationExclusion);
        Assert.True(catalog[1].EngineValid);
        Assert.False(catalog[2].EngineValid);
    }

    [Fact]
    public void RevalidatingUnchangedRowsMakesNewlyInstalledMapsAvailable()
    {
        var f = new RotationEngineTests.Fixture();
        var unchanged = f.Engine.Configuration;
        f.Engine.Configure(unchanged, [1]);
        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);
        Assert.Empty(f.Engine.NominationMaps());
        Assert.Equal("EngineRejected", f.Engine.Catalog().Single(map => map.Id == 2).NominationExclusion);

        f.Engine.Configure(unchanged, [1, 2]);
        Assert.Equal(RotationPauseReason.None, f.Engine.PauseReason);
        Assert.Equal(2, Assert.Single(f.Engine.NominationMaps()).Id);
        Assert.Null(f.Engine.Catalog().Single(map => map.Id == 2).NominationExclusion);
    }

    [Fact]
    public void CurrentMapBecomesVisibleOnlyWhenRepetitionIsAllowed()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, [f.Maps[0]]), [1]);
        Assert.Equal("CurrentMap", Assert.Single(f.Engine.Catalog()).NominationExclusion);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { AllowSameMap = true }, [f.Maps[0]]), [1]);
        Assert.Null(Assert.Single(f.Engine.Catalog()).NominationExclusion);
        Assert.Single(f.Engine.NominationMaps());
    }

    [Fact]
    public void NominationDiagnosticsDistinguishGlobalSettingsAndRecentMaps()
    {
        var f = new RotationEngineTests.Fixture(new() { RecentMapsExcluded = 1, VoteOptionsCount = 1, NominationSlots = 1 });
        f.Engine.LoadMap("de_current", "", f.Engine.Checkpoint() with { History = [2] });
        Assert.Equal("RecentMap", f.Engine.Catalog().Single(map => map.Id == 2).NominationExclusion);
        Assert.Null(f.Engine.Catalog().Single(map => map.Id == 3).NominationExclusion);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { NominationsEnabled = false }, f.Maps), [1, 2, 3, 4]);
        Assert.Equal("NominationsDisabled", f.Engine.Catalog().Single(map => map.Id == 3).NominationExclusion);
    }
}
