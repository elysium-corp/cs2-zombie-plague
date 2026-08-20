using Admin.Api.Data;
using Admin.Core.Data;

namespace Admin.Core.Services;

/// <summary>
/// Загружает определения привилегий из persistent-хранилища.
/// </summary>
internal interface IPrivilegePersistenceService
{
    /// <summary>
    /// Загружает все существующие привилегии вместе с входящими
    /// в них разрешениями.
    /// </summary>
    Task<IReadOnlyCollection<PrivilegeDefinition>> LoadAsync(CancellationToken cancellationToken = default);
}