using Admin.Core.Managers;
using Microsoft.Extensions.Logging;

namespace Admin.Core.Services;

/// <summary>
/// Периодически обновляет runtime-привилегии онлайн-игроков
/// из persistent-хранилища.
/// </summary>
internal sealed class PlayerPrivilegeRefreshService(
    IPrivilegeCatalogService privilegeCatalogService,
    IPlayerPrivilegeManager playerPrivilegeManager,
    ILogger<PlayerPrivilegeRefreshService> logger) : IPlayerPrivilegeRefreshService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    private readonly CancellationTokenSource _cancellation = new();

    private Task? _refreshTask;

    /// <inheritdoc />
    public void Start()
    {
        if (_refreshTask != null)
        {
            return;
        }

        _refreshTask = RunAsync(_cancellation.Token);
    }

    /// <inheritdoc />
    public void StopAndWait()
    {
        _cancellation.Cancel();

        if (_refreshTask == null)
        {
            return;
        }

        try
        {
            _refreshTask
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение фонового цикла при выгрузке плагина.
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Плагин выгружается.
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await privilegeCatalogService
                .ReloadAsync()
                .ConfigureAwait(false);

            await playerPrivilegeManager
                .ReloadAllAsync()
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to refresh admin privileges!"
            );
        }
    }
}