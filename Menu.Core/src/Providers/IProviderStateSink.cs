using Menu.Api.Providers;

namespace Menu.Core.Providers;

internal enum ProviderRejectionStatus
{
    ApiOutdated,
    Incompatible
}

/// <summary>
/// Принимает best-effort события registry. Реализация не должна блокировать
/// lifecycle Provider ожиданием БД.
/// </summary>
internal interface IProviderStateSink
{
    void ProviderRegistered(MenuProviderDescriptor descriptor, Guid sessionId, long generation);

    void ProviderRejected(
        MenuProviderDescriptor descriptor,
        Guid sessionId,
        long generation,
        ProviderRejectionStatus status,
        string errorCode);

    void MenuDeclared(string providerKey, Guid sessionId, long generation, MenuProviderMenuDescriptor descriptor);

    void ActionDeclared(string providerKey, Guid sessionId, long generation, MenuProviderActionDescriptor descriptor);

    void ExportRemoved(string providerKey, Guid sessionId, long generation, string exportType, string exportKey);

    void ProviderOffline(string providerKey, Guid sessionId, long generation);
}

internal sealed class NullProviderStateSink : IProviderStateSink
{
    public void ProviderRegistered(MenuProviderDescriptor descriptor, Guid sessionId, long generation) { }

    public void ProviderRejected(
        MenuProviderDescriptor descriptor,
        Guid sessionId,
        long generation,
        ProviderRejectionStatus status,
        string errorCode) { }

    public void MenuDeclared(string providerKey, Guid sessionId, long generation, MenuProviderMenuDescriptor descriptor) { }

    public void ActionDeclared(string providerKey, Guid sessionId, long generation, MenuProviderActionDescriptor descriptor) { }

    public void ExportRemoved(string providerKey, Guid sessionId, long generation, string exportType, string exportKey) { }

    public void ProviderOffline(string providerKey, Guid sessionId, long generation) { }
}
