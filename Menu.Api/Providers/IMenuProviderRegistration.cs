using Menu.Api.Results;

namespace Menu.Api.Providers;

/// <summary>
/// Управляет экспортами одной конкретной загрузки Provider.
/// </summary>
/// <remarks>
/// После <see cref="UnregisterProvider"/> или <see cref="IDisposable.Dispose"/>
/// Menu.Core удаляет runtime-делегаты, а server-aware запись persistence переводит
/// в Offline без удаления истории и экспортов.
/// </remarks>
public interface IMenuProviderRegistration : IDisposable
{
    /// <summary>Возвращает технический ключ Provider.</summary>
    string ProviderKey { get; }

    /// <summary>Показывает, что handle ещё владеет активной регистрацией.</summary>
    bool IsRegistered { get; }

    /// <summary>Возвращает результат первоначальной регистрации Provider.</summary>
    MenuOperationResult RegistrationResult { get; }

    /// <summary>Регистрирует или атомарно заменяет меню этой загрузки Provider.</summary>
    /// <param name="descriptor">Описание меню с handler.</param>
    /// <returns>Результат полной проверки и регистрации.</returns>
    MenuOperationResult RegisterMenu(MenuProviderMenuDescriptor descriptor);

    /// <summary>Регистрирует или атомарно заменяет действие этой загрузки Provider.</summary>
    /// <param name="descriptor">Описание действия с обязательными validator и handler.</param>
    /// <returns>Результат полной проверки и регистрации.</returns>
    MenuOperationResult RegisterAction(MenuProviderActionDescriptor descriptor);

    /// <summary>Удаляет runtime-делегат экспортированного меню.</summary>
    /// <param name="menuKey">Технический ключ меню внутри Provider.</param>
    /// <returns>Результат удаления.</returns>
    MenuOperationResult UnregisterMenu(string menuKey);

    /// <summary>Удаляет runtime-делегаты экспортированного действия.</summary>
    /// <param name="actionKey">Технический ключ действия внутри Provider.</param>
    /// <returns>Результат удаления.</returns>
    MenuOperationResult UnregisterAction(string actionKey);

    /// <summary>Выгружает Provider и безопасно удаляет все его runtime-делегаты.</summary>
    /// <returns>Результат выгрузки; повторный вызов возвращает Disposed.</returns>
    MenuOperationResult UnregisterProvider();
}
