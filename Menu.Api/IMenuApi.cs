using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Extensions;
using Menu.Api.Providers;
using Menu.Api.Results;
using SwiftlyS2.Shared.Players;

namespace Menu.Api;

/// <summary>
/// Предоставляет публичный runtime API централизованной системы меню.
/// </summary>
/// <remarks>
/// Операции открытия работают с уже подготовленным snapshot в памяти и не должны
/// выполнять запросы к базе данных, HTTP-запросы или разбор файлов.
/// Default-реализации новых членов сохраняют совместимость со старыми версиями
/// <c>Menu.Core</c> и возвращают результат <see cref="MenuOperationStatus.Unsupported"/>.
/// </remarks>
public interface IMenuApi
{
    /// <summary>
    /// Возвращает устаревший диспетчер расширений программных меню.
    /// </summary>
    IMenuExtensionDispatcher Dispatcher { get; }

    /// <summary>
    /// Возвращает устаревший реестр расширений программных меню.
    /// </summary>
    IMenuExtensionRegistry Extensions { get; }

    /// <summary>
    /// Регистрирует Provider и возвращает handle его текущей загрузки.
    /// </summary>
    /// <param name="descriptor">Описание Provider без состояния БД и целевого сервера.</param>
    /// <returns>
    /// Handle регистрации. Некорректные технические идентификаторы не приводят
    /// к исключению: причина отказа доступна через результат регистрации handle.
    /// </returns>
    IMenuProviderRegistration RegisterProvider(MenuProviderDescriptor descriptor)
    {
        return UnsupportedMenuProviderRegistration.Create(descriptor?.ProviderKey);
    }

    /// <summary>
    /// Открывает опубликованное меню вызвавшему игроку.
    /// </summary>
    /// <param name="caller">Игрок, инициировавший открытие.</param>
    /// <param name="menuKey">Стабильный технический ключ меню.</param>
    /// <returns>Результат открытия без исключения для ожидаемых runtime-ошибок.</returns>
    MenuOperationResult OpenMenu(IPlayer caller, string menuKey)
    {
        return MenuOperationResult.Unsupported("menu_api_not_implemented");
    }

    /// <summary>
    /// Открывает опубликованное меню с явно заданной аудиторией.
    /// </summary>
    /// <param name="request">Запрос с инициатором, меню и необязательным переопределением аудитории.</param>
    /// <returns>Результат открытия без исключения для ожидаемых runtime-ошибок.</returns>
    MenuOperationResult OpenMenu(MenuOpenRequest request)
    {
        if (request?.Caller is null)
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidRequest,
                "caller_required",
                "Не указан игрок, инициировавший открытие меню."
            );
        }

        if (request.AudienceOverride is not null || request.ExplicitTargets is not null)
        {
            return MenuOperationResult.Unsupported("audience_override_not_implemented");
        }

        return OpenMenu(request.Caller, request.MenuKey);
    }

    /// <summary>
    /// Открывает программное меню, экспортированное указанным Provider.
    /// </summary>
    /// <param name="caller">Игрок, инициировавший открытие и являющийся получателем.</param>
    /// <param name="providerKey">Стабильный технический ключ Provider.</param>
    /// <param name="menuKey">Стабильный технический ключ меню внутри Provider.</param>
    /// <returns>Результат открытия, включая безопасный статус недоступного Provider.</returns>
    MenuOperationResult OpenProviderMenu(IPlayer caller, string providerKey, string menuKey)
    {
        return MenuOperationResult.Unsupported("menu_api_not_implemented");
    }

    /// <summary>
    /// Возвращает manifest возможностей Menu.Core и установленного Swiftly Menu API.
    /// </summary>
    /// <returns>Manifest текущего целевого сервера.</returns>
    MenuCapabilityManifest GetCapabilities()
    {
        return MenuCapabilityManifest.Unsupported;
    }

    /// <summary>
    /// Ключ общей регистрации API.
    /// </summary>
    static readonly string SharedApiKey = "Menu.Api.IMenuApi";
}
