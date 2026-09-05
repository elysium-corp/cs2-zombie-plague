namespace SupplyBox.Configuration;

internal sealed record SupplyBoxSnapshot(long Version, SupplyBoxDocument Document);

internal sealed record SupplyBoxConfigurationState(
    SupplyBoxSnapshot Snapshot, string Source,
    Exception? DatabaseError = null, Exception? FallbackError = null);

internal static class SupplyBoxConfigurationLoader
{
    public static async Task<SupplyBoxConfigurationState> LoadAsync(
        Func<CancellationToken, Task<SupplyBoxSnapshot>> readDatabase,
        Func<CancellationToken, Task<SupplyBoxDocument?>> readFallback,
        SupplyBoxConfigurationState previous,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var snapshot = await readDatabase(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            snapshot.Document.Validate();
            return new(snapshot, "database");
        }
        catch (Exception databaseError) when (!token.IsCancellationRequested)
        {
            Exception? fallbackError = null;
            try
            {
                // Резервный файл читается только после неудачной попытки загрузки БД
                var fallback = await readFallback(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (fallback is not null)
                {
                    fallback.Validate();
                    return new(new(0, fallback), "fallback", databaseError);
                }
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                fallbackError = exception;
            }
            return new(previous.Snapshot, previous.Source == "loading" ? "defaults" : "memory", databaseError, fallbackError);
        }
    }
}
