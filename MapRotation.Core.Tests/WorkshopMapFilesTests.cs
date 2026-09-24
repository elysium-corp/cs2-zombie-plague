using Xunit;

namespace MapRotation.Core.Tests;

public sealed class WorkshopMapFilesTests
{
    private const long WorkshopId = 3764581596;

    [Theory]
    [InlineData("3764581596.vpk")]
    [InlineData("3764581596_dir.vpk")]
    [InlineData("zm_lila_hacker_meow_v3.vpk")]
    [InlineData("zm_lila_hacker_meow_v3_dir.vpk")]
    public void InstalledMapUsesTheDirectoryReportedBySteam(string filename)
    {
        using var folder = new TemporaryFolder();
        var path = folder.Write(filename);
        var files = new WorkshopMapFiles(id =>
        {
            Assert.Equal((ulong)WorkshopId, id);
            return 4;
        }, id =>
        {
            Assert.Equal((ulong)WorkshopId, id);
            return folder.Path;
        });

        var result = files.Read(WorkshopId);

        Assert.True(result.IsReady);
        Assert.Equal(4u, result.ItemState);
        Assert.Equal(folder.Path, result.InstallDirectory);
        Assert.Equal(path, result.VpkPath);
        Assert.Null(result.ErrorType);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(8u)]
    [InlineData(4u | 2u)]
    [InlineData(4u | 16u)]
    [InlineData(4u | 32u)]
    [InlineData(4u | 8u | 16u | 32u)]
    public void UnavailableStatesNeverReadTheInstallDirectory(uint state)
    {
        var files = new WorkshopMapFiles(_ => state, _ => throw new Xunit.Sdk.XunitException("Каталог не должен читаться"));

        var result = files.Read(WorkshopId);

        Assert.False(result.IsReady);
        Assert.Equal(state, result.ItemState);
        Assert.Null(result.InstallDirectory);
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void InstalledOldVersionRemainsUsableBeforeDownloadStarts()
    {
        using var folder = new TemporaryFolder();
        folder.Write("3764581596_dir.vpk");

        Assert.True(new WorkshopMapFiles(_ => 4 | 8, _ => folder.Path).Read(WorkshopId).IsReady);
    }

    [Fact]
    public void MissingEmptyNestedAndChunkFilesAreNotUsableArchives()
    {
        using var folder = new TemporaryFolder();
        var files = new WorkshopMapFiles(_ => 4, _ => folder.Path);
        Assert.False(files.Read(WorkshopId).IsReady);

        folder.Write("3764581596.vpk", []);
        folder.Write("3764581596_dir.vpk", []);
        folder.Write("3764581596_000.vpk");
        folder.Write("zm_map_001.vpk");
        folder.Write("notes.txt");
        Directory.CreateDirectory(System.IO.Path.Combine(folder.Path, "nested"));
        folder.Write("nested/3764581596_dir.vpk");

        var result = files.Read(WorkshopId);
        Assert.False(result.IsReady);
        Assert.Null(result.ErrorType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingInstallInfoDoesNotBecomeReady(string? directory)
    {
        Assert.False(new WorkshopMapFiles(_ => 4, _ => directory).Read(WorkshopId).IsReady);
    }

    [Fact]
    public void MissingNativeBindingDoesNotPreventCheckingAnotherMap()
    {
        using var folder = new TemporaryFolder();
        folder.Write("zm_map.vpk");
        var files = new WorkshopMapFiles(id => id == (ulong)WorkshopId
            ? throw new DllNotFoundException() : 4u, _ => folder.Path);

        var failed = files.Read(WorkshopId);
        Assert.False(failed.IsReady);
        Assert.Null(failed.ItemState);
        Assert.Equal(nameof(DllNotFoundException), failed.ErrorType);
        Assert.True(files.Read(3100743780).IsReady);
    }

    [Fact]
    public void SteamNotInitializedIsReportedLocally()
    {
        var result = new WorkshopMapFiles(_ => throw new InvalidOperationException(), _ => null).Read(WorkshopId);

        Assert.False(result.IsReady);
        Assert.Equal(nameof(InvalidOperationException), result.ErrorType);
    }

    [Fact]
    public void InstallInfoErrorPreservesTheKnownItemState()
    {
        var result = new WorkshopMapFiles(_ => 4, _ => throw new EntryPointNotFoundException()).Read(WorkshopId);

        Assert.False(result.IsReady);
        Assert.Equal(4u, result.ItemState);
        Assert.Equal(nameof(EntryPointNotFoundException), result.ErrorType);
    }

    [Fact]
    public void RemovedInstallDirectoryIsReportedWithoutThrowing()
    {
        using var folder = new TemporaryFolder();
        var missing = System.IO.Path.Combine(folder.Path, "missing");

        var result = new WorkshopMapFiles(_ => 4, _ => missing).Read(WorkshopId);

        Assert.False(result.IsReady);
        Assert.Equal(missing, result.InstallDirectory);
        Assert.Equal(nameof(DirectoryNotFoundException), result.ErrorType);
    }

    [Fact]
    public void RelativeInstallDirectoryIsRejected()
    {
        var result = new WorkshopMapFiles(_ => 4, _ => "workshop/3764581596").Read(WorkshopId);

        Assert.False(result.IsReady);
        Assert.Equal(nameof(ArgumentException), result.ErrorType);
    }

    private sealed class TemporaryFolder : IDisposable
    {
        internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "maprotation-workshop-" + Guid.NewGuid().ToString("N"));

        internal TemporaryFolder() => Directory.CreateDirectory(Path);

        internal string Write(string filename, byte[]? contents = null)
        {
            var path = System.IO.Path.Combine(Path, filename);
            File.WriteAllBytes(path, contents ?? [1, 2, 3, 4]);
            return path;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
