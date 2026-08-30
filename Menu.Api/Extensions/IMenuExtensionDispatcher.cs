namespace Menu.Api.Extensions;

/// <summary>
/// Диспетчер устаревшего механизма динамического дополнения программных меню.
/// </summary>
public interface IMenuExtensionDispatcher
{
    /// <summary>
    /// Последовательно вызывает зарегистрированные расширения указанного меню.
    /// </summary>
    /// <param name="menuId">Технический идентификатор программного меню.</param>
    /// <param name="context">Игрок и изменяемая коллекция пунктов меню.</param>
    void Dispatch(string menuId, MenuExtensionContext context);
}
