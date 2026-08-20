namespace Admin.Core.Services;

/// <summary>
/// Синхронизирует runtime-каталог привилегий с базой данных.
/// </summary>
internal interface IPrivilegeCatalogService
{
    /// <summary>
    /// Полностью перезагружает определения привилегий и их разрешения.
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}