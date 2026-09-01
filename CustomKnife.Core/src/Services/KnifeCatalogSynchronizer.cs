using CustomKnife.Data.Registrator;
using CustomKnife.Database;
using Microsoft.Extensions.Logging;

namespace CustomKnife.Services;

internal sealed class KnifeCatalogSynchronizer(
    IKnifeCatalogRepository repository,
    IWritableKnivesRegistry registry,
    ILogger<KnifeCatalogSynchronizer> logger
) : IDisposable
{
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public bool TryReload(out int count)
    {
        count = 0;
        _reloadLock.Wait();

        try
        {
            var knives = repository.GetEnabledKnives();
            registry.ReplaceAll(knives);
            count = knives.Count;
            logger.LogInformation("Loaded {KnifeCount} enabled custom knives from PostgreSQL.", count);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to load custom knives. The previous in-memory snapshot is still active."
            );
            return false;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose()
    {
        _reloadLock.Dispose();
    }
}
