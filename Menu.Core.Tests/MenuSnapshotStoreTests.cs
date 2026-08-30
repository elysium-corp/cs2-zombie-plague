using System.Collections.Concurrent;
using Menu.Api.Enums;
using Menu.Core.Runtime;
using Menu.Core.Validation;

namespace Menu.Core.Tests;

public sealed class MenuSnapshotStoreTests
{
    [Fact]
    public void TryActivate_InvalidCandidateNeverReplacesCurrentSnapshot()
    {
        var store = CreateStore();
        var context = TestReleaseFactory.Context();
        var currentRelease = TestReleaseFactory.Release(1);
        var initial = store.TryActivate(
            currentRelease,
            context,
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow);
        var current = initial.Snapshot;
        var draft = TestReleaseFactory.Release(
            2,
            [TestReleaseFactory.Menu(status: MenuLifecycleStatus.Draft)]);

        var rejected = store.TryActivate(
            draft,
            context,
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.True(initial.Activated);
        Assert.False(rejected.Activated);
        Assert.Same(current, store.Current);
        Assert.Same(current, rejected.Snapshot);
        Assert.Equal(1L, store.Status.ActiveReleaseId);
        Assert.False(store.Status.LastAttemptSucceeded);
    }

    [Fact]
    public void TryActivate_CompilesOnlyMenusAndCommandsApplicableToServer()
    {
        var global = TestReleaseFactory.Menu("global");
        var server = TestReleaseFactory.Menu(
            "server",
            scope: TestReleaseFactory.ServerScope("zombie-1"));
        var otherServer = TestReleaseFactory.Menu(
            "other-server",
            scope: TestReleaseFactory.ServerScope("zombie-2"));
        var group = TestReleaseFactory.Menu(
            "group",
            scope: TestReleaseFactory.GroupScope("zombie"));
        var otherGroup = TestReleaseFactory.Menu(
            "other-group",
            scope: TestReleaseFactory.GroupScope("classic"));
        var release = TestReleaseFactory.Release(
            menus: [global, server, otherServer, group, otherGroup],
            commands:
            [
                TestReleaseFactory.Command("global-command", "/global", menuKey: "global"),
                TestReleaseFactory.Command(
                    "server-command",
                    "/server",
                    menuKey: "server",
                    scope: TestReleaseFactory.ServerScope("zombie-1")),
                TestReleaseFactory.Command(
                    "other-server-command",
                    "/other-server",
                    menuKey: "other-server",
                    scope: TestReleaseFactory.ServerScope("zombie-2")),
                TestReleaseFactory.Command(
                    "group-command",
                    "/group",
                    menuKey: "group",
                    scope: TestReleaseFactory.GroupScope("zombie")),
                TestReleaseFactory.Command(
                    "other-group-command",
                    "/other-group",
                    menuKey: "other-group",
                    scope: TestReleaseFactory.GroupScope("classic"))
            ]);
        var store = CreateStore();

        var activation = store.TryActivate(
            release,
            TestReleaseFactory.Context(),
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow);

        Assert.True(activation.Activated, string.Join(
            Environment.NewLine,
            activation.Validation.Errors.Select(issue => issue.Code)));
        Assert.Equal(
            new[] { "global", "group", "server" },
            activation.Snapshot.Menus.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.True(activation.Snapshot.TryGetCommand(MenuCommandKind.Chat, "/global", out _));
        Assert.True(activation.Snapshot.TryGetCommand(MenuCommandKind.Chat, "/server", out _));
        Assert.True(activation.Snapshot.TryGetCommand(MenuCommandKind.Chat, "/group", out _));
        Assert.False(activation.Snapshot.TryGetCommand(MenuCommandKind.Chat, "/other-server", out _));
        Assert.False(activation.Snapshot.TryGetCommand(MenuCommandKind.Chat, "/other-group", out _));
    }

    [Fact]
    public async Task Current_ReadersNeverObserveTornSnapshotDuringConcurrentSwaps()
    {
        const int releaseCount = 80;
        var context = TestReleaseFactory.Context();
        var releases = Enumerable.Range(1, releaseCount)
            .Select(id => TestReleaseFactory.Release(
                id,
                [TestReleaseFactory.Menu($"menu-{id}")]))
            .ToArray();
        var expected = releases.ToDictionary(
            release => release.ReleaseId,
            release => (release.Checksum!, MenuKey: release.Menus.Single().MenuKey));
        var store = CreateStore();
        Assert.True(store.TryActivate(
            releases[0],
            context,
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow).Activated);

        var failures = new ConcurrentQueue<string>();
        var completed = 0;
        var readers = Enumerable.Range(0, Math.Max(4, Environment.ProcessorCount))
            .Select(_ => Task.Run(() =>
            {
                while (Volatile.Read(ref completed) == 0)
                {
                    Inspect(store.Current, expected, failures);
                }

                Inspect(store.Current, expected, failures);
            }))
            .ToArray();

        var writer = Task.Run(() =>
        {
            try
            {
                for (var index = 1; index < releases.Length; index++)
                {
                    var result = store.TryActivate(
                        releases[index],
                        context,
                        MenuSnapshotSource.Database,
                        DateTimeOffset.UtcNow.AddMilliseconds(index));
                    if (!result.Activated)
                    {
                        failures.Enqueue($"Release {releases[index].ReleaseId} was rejected.");
                    }
                }
            }
            finally
            {
                Volatile.Write(ref completed, 1);
            }
        });

        await Task.WhenAll(readers.Append(writer));

        Assert.Empty(failures);
        Assert.Equal((long)releaseCount, store.Current.ReleaseId);
    }

    private static MenuSnapshotStore CreateStore()
    {
        return new MenuSnapshotStore(new MenuReleaseValidator(), new MenuSnapshotCompiler());
    }

    private static void Inspect(
        MenuRuntimeSnapshot snapshot,
        IReadOnlyDictionary<long, (string Checksum, string MenuKey)> expected,
        ConcurrentQueue<string> failures)
    {
        if (!expected.TryGetValue(snapshot.ReleaseId, out var release))
        {
            failures.Enqueue($"Unknown release {snapshot.ReleaseId}.");
            return;
        }

        if (!string.Equals(snapshot.Checksum, release.Checksum, StringComparison.Ordinal) ||
            snapshot.Menus.Count != 1 ||
            !snapshot.Menus.ContainsKey(release.MenuKey))
        {
            failures.Enqueue($"Torn snapshot observed for release {snapshot.ReleaseId}.");
        }
    }
}
