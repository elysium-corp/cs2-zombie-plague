using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Core.Application;
using Menu.Core.Configuration;
using Menu.Core.Providers;
using Menu.Core.Runtime;
using Menu.Core.Storage;
using Menu.Core.Swiftly;
using Menu.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Menu.Core.Tests;

public sealed class MenuBootstrapLoaderTests
{
    [Fact]
    public async Task TryActivateLocal_PrefersValidLastKnownGoodOverFallback()
    {
        using var directory = new TemporaryDirectory();
        var context = TestReleaseFactory.Context();
        var (loader, fileStore, snapshotStore) = CreateLoader();
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        Assert.True((await fileStore.SaveValidatedAsync(
            lkgPath,
            TestReleaseFactory.Release(10),
            context)).IsValid);
        Assert.True((await fileStore.SaveValidatedAsync(
            fallbackPath,
            TestReleaseFactory.Release(20),
            context)).IsValid);

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            context,
            DateTimeOffset.UtcNow);

        Assert.True(result.Activated);
        Assert.Equal(MenuSnapshotSource.LastKnownGood, result.Source);
        Assert.Equal(10L, result.Snapshot.ReleaseId);
        Assert.Single(result.Attempts);
        Assert.Same(result.Snapshot, snapshotStore.Current);
    }

    [Fact]
    public async Task TryActivateLocal_FallsBackWhenLastKnownGoodJsonIsCorrupt()
    {
        using var directory = new TemporaryDirectory();
        var context = TestReleaseFactory.Context();
        var (loader, fileStore, _) = CreateLoader();
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        await File.WriteAllTextAsync(lkgPath, "{broken-json");
        Assert.True((await fileStore.SaveValidatedAsync(
            fallbackPath,
            TestReleaseFactory.Release(20),
            context)).IsValid);

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            context,
            DateTimeOffset.UtcNow);

        Assert.True(result.Activated);
        Assert.Equal(MenuSnapshotSource.Fallback, result.Source);
        Assert.Equal(20L, result.Snapshot.ReleaseId);
        Assert.Collection(
            result.Attempts,
            attempt =>
            {
                Assert.Equal(MenuSnapshotSource.LastKnownGood, attempt.Source);
                Assert.False(attempt.Activated);
                Assert.Contains(attempt.Issues, issue => issue.Code == "file.json_invalid");
            },
            attempt =>
            {
                Assert.Equal(MenuSnapshotSource.Fallback, attempt.Source);
                Assert.True(attempt.Activated);
            });
    }

    [Fact]
    public async Task TryActivateLocal_FallsBackWhenLastKnownGoodIsJsonNull()
    {
        using var directory = new TemporaryDirectory();
        var context = TestReleaseFactory.Context();
        var (loader, fileStore, _) = CreateLoader();
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        await File.WriteAllTextAsync(lkgPath, "null");
        Assert.True((await fileStore.SaveValidatedAsync(
            fallbackPath,
            TestReleaseFactory.Release(20),
            context)).IsValid);

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            context,
            DateTimeOffset.UtcNow);

        Assert.True(result.Activated);
        Assert.Equal(MenuSnapshotSource.Fallback, result.Source);
        Assert.Equal(20L, result.Snapshot.ReleaseId);
        Assert.Contains(result.Attempts[0].Issues, issue => issue.Code == "file.release_required");
    }

    [Fact]
    public async Task TryActivateLocal_RejectsChecksumCorruptionAndUsesFallback()
    {
        using var directory = new TemporaryDirectory();
        var context = TestReleaseFactory.Context();
        var (loader, fileStore, _) = CreateLoader();
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        var validLkg = TestReleaseFactory.Release(10);
        var tampered = validLkg with
        {
            Menus =
            [
                validLkg.Menus.Single() with
                {
                    Title = TestReleaseFactory.Text("Changed after signing")
                }
            ]
        };
        await File.WriteAllBytesAsync(
            lkgPath,
            JsonSerializer.SerializeToUtf8Bytes(tampered, MenuJson.SerializerOptions));
        Assert.True((await fileStore.SaveValidatedAsync(
            fallbackPath,
            TestReleaseFactory.Release(20),
            context)).IsValid);

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            context,
            DateTimeOffset.UtcNow);

        Assert.True(result.Activated);
        Assert.Equal(MenuSnapshotSource.Fallback, result.Source);
        Assert.Contains(result.Attempts[0].Issues, issue => issue.Code == "release.checksum_mismatch");
    }

    [Fact]
    public async Task TryActivateLocal_KeepsExistingSnapshotWhenBothFilesAreCorrupt()
    {
        using var directory = new TemporaryDirectory();
        var context = TestReleaseFactory.Context();
        var (loader, _, snapshotStore) = CreateLoader();
        var active = snapshotStore.TryActivate(
            TestReleaseFactory.Release(5),
            context,
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow).Snapshot;
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        await File.WriteAllTextAsync(lkgPath, "not json");
        await File.WriteAllTextAsync(fallbackPath, "still not json");

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            context,
            DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.False(result.Activated);
        Assert.Equal(MenuSnapshotSource.None, result.Source);
        Assert.Equal(2, result.Attempts.Length);
        Assert.Same(active, result.Snapshot);
        Assert.Same(active, snapshotStore.Current);
    }

    [Fact]
    public async Task TryActivateLocal_UsesEmbeddedProviderContractsDuringColdBootstrap()
    {
        using var directory = new TemporaryDirectory();
        var providerMenu = TestReleaseFactory.TextItem(
            "store",
            TestReleaseFactory.OpenProviderMenu("economy", "store"));
        var providerAction = TestReleaseFactory.TextItem(
            "purchase",
            new MenuActionDefinition
            {
                Kind = MenuActionKind.ProviderAction,
                ProviderKey = "economy",
                ProviderActionKey = "purchase",
                Arguments = TestReleaseFactory.Json(new { sku = "health" })
            });
        var releaseWithoutChecksum = TestReleaseFactory.Release(
            30,
            [TestReleaseFactory.Menu(items: [providerMenu, providerAction])]) with
        {
            Metadata = new Dictionary<string, JsonElement>
            {
                ["providerContracts"] = TestReleaseFactory.Json(new[]
                {
                    new
                    {
                        providerKey = "economy",
                        menuApiVersion = MenuContractVersions.MenuCoreApiVersion,
                        menuKeys = new[] { "store" },
                        actionKeys = new[] { "purchase" }
                    }
                })
            }
        };
        var release = TestReleaseFactory.WithChecksum(releaseWithoutChecksum);
        var lkgPath = directory.File("lkg.json");
        var fallbackPath = directory.File("fallback.json");
        await File.WriteAllBytesAsync(
            lkgPath,
            JsonSerializer.SerializeToUtf8Bytes(release, MenuJson.SerializerOptions));

        var options = Options.Create(new MenuCoreConfig
        {
            ServerKey = "zombie-1",
            ServerGroups = ["zombie"]
        });
        var registry = new ProviderRegistry(
            new NullProviderStateSink(),
            NullLogger<ProviderRegistry>.Instance);
        var contextFactory = new MenuValidationContextFactory(
            registry,
            new MenuCapabilityProvider(options),
            options);
        var (loader, _, _) = CreateLoader();

        var result = await loader.TryActivateLocalAsync(
            lkgPath,
            fallbackPath,
            artifact => contextFactory.Create(artifact),
            DateTimeOffset.UtcNow);

        Assert.True(result.Activated, string.Join(
            Environment.NewLine,
            result.Attempts.SelectMany(attempt => attempt.Issues).Select(issue => issue.Code)));
        Assert.Equal(MenuSnapshotSource.LastKnownGood, result.Source);
        Assert.Equal(30L, result.Snapshot.ReleaseId);
        Assert.Equal(
            2,
            result.Attempts.Single().Issues.Count(issue => issue.Code == "provider.offline"));
    }

    private static (MenuBootstrapLoader Loader, MenuReleaseFileStore FileStore, MenuSnapshotStore SnapshotStore)
        CreateLoader()
    {
        var validator = new MenuReleaseValidator();
        var snapshotStore = new MenuSnapshotStore(validator, new MenuSnapshotCompiler());
        var fileStore = new MenuReleaseFileStore(validator);
        return (new MenuBootstrapLoader(fileStore, snapshotStore), fileStore, snapshotStore);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(
            Path.GetTempPath(),
            "menu-core-tests",
            Guid.NewGuid().ToString("N"));

        internal TemporaryDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        internal string File(string name)
        {
            return Path.Combine(_path, name);
        }

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }
}
