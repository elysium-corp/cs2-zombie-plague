using System.Collections.Immutable;
using SwiftlyS2.Shared.Players;

namespace CustomHud.Api;

/// <summary>Приоритет полноэкранного меню; Critical нельзя вытеснить меню Normal.</summary>
public enum HudMenuPriority { Normal, Critical }
/// <summary>Нормализованное действие пользователя.</summary>
public enum HudMenuAction { Select, Close, NextPage, PreviousPage, Back, SettingsChanged }
/// <summary>Режим отображения общего шаблона.</summary>
public enum HudMenuView { List, Result }

/// <summary>Расположение пунктов меню.</summary>
public enum HudMenuOrientation
{
    /// <summary>Вертикальный список.</summary>
    Vertical,
    /// <summary>Горизонтальная сетка с пятью карточками в строке.</summary>
    Horizontal
}

/// <summary>Персональный вид меню; изменение не меняет пункты, страницу или выбор.</summary>
public sealed record HudMenuPresentation
{
    /// <summary>Ориентация списка; по умолчанию сохраняется вертикальный вид существующих потребителей.</summary>
    public HudMenuOrientation Orientation { get; init; }
    /// <summary>Масштаб 80, 100 или 120 процентов.</summary>
    public int ScalePercent { get; init; } = 100;
}

/// <summary>Локализованные подписи личных настроек; все строки получает вызывающий модуль через Localization.Api.</summary>
public sealed record HudMenuSettingsText
{
    /// <summary>Заголовок окна настроек.</summary>
    public required string Title { get; init; }
    /// <summary>Название настройки ориентации.</summary>
    public required string Orientation { get; init; }
    /// <summary>Подпись горизонтального вида.</summary>
    public required string Horizontal { get; init; }
    /// <summary>Подпись вертикального вида.</summary>
    public required string Vertical { get; init; }
    /// <summary>Название настройки масштаба.</summary>
    public required string Size { get; init; }
    /// <summary>Подпись масштаба 80 процентов.</summary>
    public required string Scale80 { get; init; }
    /// <summary>Подпись масштаба 100 процентов.</summary>
    public required string Scale100 { get; init; }
    /// <summary>Подпись масштаба 120 процентов.</summary>
    public required string Scale120 { get; init; }
}

/// <summary>Один пункт меню. Текст передаётся уже локализованным, без HTML.</summary>
public sealed record HudMenuItem(string Id, string Title, string Description = "", string Badge = "",
    bool Enabled = true, string DisabledReason = "", bool Selected = false)
{
    /// <summary>Необязательная текстура из общей библиотеки CMS; исходник должен быть включён в VPK.</summary>
    public string? ImagePath { get; init; }
}

/// <summary>Параметры поведения меню.</summary>
public sealed record HudMenuOptions
{
    /// <summary>Затемнять фон за окном.</summary>
    public bool Modal { get; init; } = true;
    /// <summary>Приоритет по отношению к другим каналам.</summary>
    public HudMenuPriority Priority { get; init; }
    /// <summary>Разрешить ESC и кнопку закрытия.</summary>
    public bool Closable { get; init; } = true;
    /// <summary>Захватывать ввод только владельца меню.</summary>
    public bool CaptureInput { get; init; } = true;
    /// <summary>Число пунктов страницы, от 1 до 10.</summary>
    public int ItemsPerPage { get; init; } = 5;
    /// <summary>Показывать навигацию, когда страниц больше одной.</summary>
    public bool ShowPagination { get; init; } = true;
    /// <summary>Закрыть меню до вызова обработчика выбора.</summary>
    public bool CloseOnSelect { get; init; }
}

/// <summary>Неизменяемая модель меню, независимая от сущностей Panorama.</summary>
public sealed record HudMenu(string Channel, string Title, string Subtitle,
    ImmutableArray<HudMenuItem> Items, HudMenuOptions Options)
{
    /// <summary>Необязательный класс оформления из скомпилированного CSS, без пробелов и селекторов.</summary>
    public string StyleClass { get; init; } = "";
    /// <summary>Личный вид; рендерер сохраняет его в событии SettingsChanged, а хранение выполняет потребитель.</summary>
    public HudMenuPresentation Presentation { get; init; } = new();
    /// <summary>Наличие подписей разрешает шестерёнку; null скрывает настройки. Для результата настройки всегда скрыты.</summary>
    public HudMenuSettingsText? SettingsText { get; init; }
    /// <summary>Показывать бренд в шапке; для результата бренд всегда скрыт.</summary>
    public bool ShowBrand { get; init; }
    /// <summary>Список или карточка результата.</summary>
    public HudMenuView View { get; init; }
    /// <summary>Текст справа сверху, например оставшееся время.</summary>
    public string Status { get; init; } = "";
    /// <summary>Подпись закрытия, полученная вызывающим модулем через Localization.Api; пустая строка не задаёт подпись.</summary>
    public string CloseText { get; init; } = "";
    /// <summary>Информационная строка под содержимым.</summary>
    public string Footer { get; init; } = "";
    /// <summary>Показывать кнопку Back.</summary>
    public bool ShowBack { get; init; }
}

/// <summary>Идентификатор открытия и выбранный пункт; старый идентификатор нельзя использовать для нового меню.</summary>
public sealed record HudMenuEvent(Guid MenuId, HudMenuAction Action, string? ItemId)
{
    /// <summary>Новый персональный вид только для SettingsChanged; не является выбором пункта меню.</summary>
    public HudMenuPresentation? Presentation { get; init; }
}

/// <summary>Общие меню Elysium. Только игровой поток; один активный fullscreen menu на игрока.</summary>
public interface ICustomHudMenuApi
{
    /// <summary>Ключ shared-интерфейса, независимый от сообщений и баннеров.</summary>
    public const string SharedApiKey = "CustomHud.Api.ICustomHudMenuApi";
    /// <summary>Вызывается перед захватом ввода. Другие владельцы fullscreen HUD закрывают свой интерфейс.</summary>
    event Action<IPlayer>? Opening;
    /// <summary>Проверяет наличие любого активного меню у текущей сессии игрока.</summary>
    bool IsAnyOpen(IPlayer player);
    /// <summary>Открывает меню; null означает недоступные ресурсы или более высокий приоритет. Обработчик вызывается на игровом потоке.</summary>
    Guid? Open(IPlayer player, HudMenu menu, Action<HudMenuEvent> onAction);
    /// <summary>Обновляет существующее открытие; не открывает закрытое и не меняет его канал.</summary>
    bool Update(IPlayer player, Guid menuId, HudMenu menu);
    /// <summary>Закрывает указанное открытие и освобождает ввод. Повторный вызов безопасен.</summary>
    void Close(IPlayer player, Guid menuId);
    /// <summary>Проверяет открытие с учётом SteamID и сессии подключения.</summary>
    bool IsOpen(IPlayer player, Guid menuId);
    /// <summary>Закрывает меню указанного канала у всех игроков.</summary>
    void CloseChannel(string channel);
}
