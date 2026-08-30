namespace Menu.Api.Extensions;

/// <summary>
/// Дополняет программное меню пунктами для конкретного игрока.
/// </summary>
/// <param name="context">Контекст построения меню.</param>
public delegate void MenuExtensionHandler(MenuExtensionContext context);
