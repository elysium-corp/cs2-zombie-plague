namespace Menu.Core.Runtime;

internal enum MenuSnapshotSource
{
    None = 0,
    Database = 1,
    LastKnownGood = 2,
    Fallback = 3
}

internal static class MenuSnapshotSourceExtensions
{
    public static string? ToStorageValue(this MenuSnapshotSource source)
    {
        return source switch
        {
            MenuSnapshotSource.None => null,
            MenuSnapshotSource.Database => "database",
            MenuSnapshotSource.LastKnownGood => "lkg",
            MenuSnapshotSource.Fallback => "fallback",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
