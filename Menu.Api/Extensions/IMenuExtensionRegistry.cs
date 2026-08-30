namespace Menu.Api.Extensions;

/// <summary>
/// Реестр устаревших обработчиков динамического дополнения программных меню.
/// </summary>
public interface IMenuExtensionRegistry
{
    /// <summary>
    /// Подписывает обработчик на построение указанного меню.
    /// </summary>
    /// <param name="menuId">Технический идентификатор программного меню.</param>
    /// <param name="handler">Обработчик, добавляющий пункты в меню.</param>
    /// <returns>Подписка, удаляемая вызовом <see cref="IDisposable.Dispose"/>.</returns>
    IDisposable Subscribe(string menuId, MenuExtensionHandler handler);
}
