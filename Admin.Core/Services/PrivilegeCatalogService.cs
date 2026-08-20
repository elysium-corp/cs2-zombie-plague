using Admin.Api.Data;
using Admin.Core.Data;
using Admin.Core.Registry;

namespace Admin.Core.Services;

internal sealed class PrivilegeCatalogService(
    IPrivilegePersistenceService persistenceService,
    IPrivilegeRegistry privilegeRegistry) : IPrivilegeCatalogService
{
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var privileges = await persistenceService
                .LoadAsync(cancellationToken)
                .ConfigureAwait(false);

            privilegeRegistry.ReplaceAll(privileges);
        }
        catch
        {
            // Для административных прав используем fail-closed:
            // если актуальный каталог получить не удалось,
            // никакая privilege не должна продолжать выдавать permissions.
            privilegeRegistry.ReplaceAll(Array.Empty<PrivilegeDefinition>());

            throw;
        }
    }
}