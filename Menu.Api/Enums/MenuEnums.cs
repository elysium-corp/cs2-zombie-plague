using System.Text.Json.Serialization;

namespace Menu.Api.Enums;

/// <summary>Определяет момент подавления исходного сообщения chat-команды.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChatSuppressionMode>))]
public enum ChatSuppressionMode : byte
{
    /// <summary>Не подавлять сообщение.</summary>
    [JsonStringEnumMemberName("none")]
    None = 0,

    /// <summary>Подавить сообщение сразу после совпадения с зарегистрированным alias.</summary>
    [JsonStringEnumMemberName("on_match")]
    OnMatch = 1,

    /// <summary>Подавить сообщение только после успешного открытия меню.</summary>
    [JsonStringEnumMemberName("on_success")]
    OnSuccess = 2
}

/// <summary>Определяет namespace командного alias.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuCommandKind>))]
public enum MenuCommandKind : byte
{
    /// <summary>Команда, вводимая в игровой чат.</summary>
    [JsonStringEnumMemberName("chat")]
    Chat = 0,

    /// <summary>Команда игровой консоли, включая alias с префиксом <c>sw_</c>.</summary>
    [JsonStringEnumMemberName("console")]
    Console = 1
}

/// <summary>Определяет жизненный цикл редакторской версии меню.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuLifecycleStatus>))]
public enum MenuLifecycleStatus : byte
{
    /// <summary>Черновик, который запрещено загружать в runtime.</summary>
    [JsonStringEnumMemberName("draft")]
    Draft = 0,

    /// <summary>Неизменяемая опубликованная revision.</summary>
    [JsonStringEnumMemberName("published")]
    Published = 1,

    /// <summary>Архивная revision, сохранённая для истории и rollback.</summary>
    [JsonStringEnumMemberName("archived")]
    Archived = 2
}

/// <summary>Определяет область применения конфигурации меню.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuScopeKind>))]
public enum MenuScopeKind : byte
{
    /// <summary>Все целевые серверы.</summary>
    [JsonStringEnumMemberName("global")]
    Global = 0,

    /// <summary>Один сервер.</summary>
    [JsonStringEnumMemberName("server")]
    Server = 1,

    /// <summary>Настроенная группа серверов.</summary>
    [JsonStringEnumMemberName("server_group")]
    ServerGroup = 2
}

/// <summary>Определяет способ проверки игровых permission через Admin.Core.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuAccessPolicyKind>))]
public enum MenuAccessPolicyKind : byte
{
    /// <summary>Доступ не требует permission.</summary>
    [JsonStringEnumMemberName("public")]
    Public = 0,

    /// <summary>Требуется одно указанное permission.</summary>
    [JsonStringEnumMemberName("permission")]
    Permission = 1,

    /// <summary>Достаточно любого permission из списка.</summary>
    [JsonStringEnumMemberName("any_of")]
    AnyOf = 2,

    /// <summary>Требуются все permission из списка.</summary>
    [JsonStringEnumMemberName("all_of")]
    AllOf = 3,

    /// <summary>Пункт наследует политику содержащего его меню.</summary>
    [JsonStringEnumMemberName("inherited")]
    Inherited = 4
}

/// <summary>Определяет представление пункта при отсутствии доступа.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuNoAccessBehavior>))]
public enum MenuNoAccessBehavior : byte
{
    /// <summary>Скрыть пункт.</summary>
    [JsonStringEnumMemberName("hide")]
    Hide = 0,

    /// <summary>Показать пункт как отключённый.</summary>
    [JsonStringEnumMemberName("disable")]
    Disable = 1,

    /// <summary>Показать пункт и при выборе вывести сообщение об отсутствии доступа.</summary>
    [JsonStringEnumMemberName("show_no_access")]
    ShowNoAccess = 2
}

/// <summary>Определяет получателей открываемого меню.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuAudienceKind>))]
public enum MenuAudienceKind : byte
{
    /// <summary>Только игрок, инициировавший открытие.</summary>
    [JsonStringEnumMemberName("caller")]
    Caller = 0,

    /// <summary>Все подключённые игроки.</summary>
    [JsonStringEnumMemberName("all_players")]
    AllPlayers = 1,

    /// <summary>Игроки текущей команды инициатора.</summary>
    [JsonStringEnumMemberName("team")]
    Team = 2,

    /// <summary>Все живые игроки.</summary>
    [JsonStringEnumMemberName("alive_players")]
    AlivePlayers = 3,

    /// <summary>Все мёртвые игроки.</summary>
    [JsonStringEnumMemberName("dead_players")]
    DeadPlayers = 4,

    /// <summary>Все наблюдатели.</summary>
    [JsonStringEnumMemberName("spectators")]
    Spectators = 5,

    /// <summary>Явно переданный набор получателей.</summary>
    [JsonStringEnumMemberName("explicit_targets")]
    ExplicitTargets = 6
}

/// <summary>Определяет тип пункта, поддерживаемый Swiftly Menu adapter.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuItemKind>))]
public enum MenuItemKind : byte
{
    /// <summary>Текстовый пункт.</summary>
    [JsonStringEnumMemberName("text")]
    Text = 0,

    /// <summary>Переключатель логического значения.</summary>
    [JsonStringEnumMemberName("checkbox")]
    Checkbox = 1,

    /// <summary>Выбор одного значения из списка.</summary>
    [JsonStringEnumMemberName("choice")]
    Choice = 2,

    /// <summary>Числовой ползунок.</summary>
    [JsonStringEnumMemberName("slider")]
    Slider = 3,

    /// <summary>Пункт специального типа C4 SwiftlyS2.</summary>
    [JsonStringEnumMemberName("c4")]
    C4 = 4
}

/// <summary>Определяет безопасное действие пункта меню.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuActionKind>))]
public enum MenuActionKind : byte
{
    /// <summary>Действие отсутствует.</summary>
    [JsonStringEnumMemberName("none")]
    None = 0,

    /// <summary>Открыть опубликованное меню текущего Release.</summary>
    [JsonStringEnumMemberName("open_menu")]
    OpenMenu = 1,

    /// <summary>Открыть программное меню зарегистрированного Provider.</summary>
    [JsonStringEnumMemberName("open_provider_menu")]
    OpenProviderMenu = 2,

    /// <summary>Выполнить зарегистрированный и валидируемый Provider Action.</summary>
    [JsonStringEnumMemberName("provider_action")]
    ProviderAction = 3,

    /// <summary>Вернуться к родительскому меню.</summary>
    [JsonStringEnumMemberName("back")]
    Back = 4,

    /// <summary>Закрыть меню.</summary>
    [JsonStringEnumMemberName("close")]
    Close = 5
}

/// <summary>Определяет представление зависимости от выгруженного Provider.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProviderUnavailableBehavior>))]
public enum ProviderUnavailableBehavior : byte
{
    /// <summary>Скрыть зависящий от Provider пункт.</summary>
    [JsonStringEnumMemberName("hide")]
    Hide = 0,

    /// <summary>Показать зависящий от Provider пункт как отключённый.</summary>
    [JsonStringEnumMemberName("disable")]
    Disable = 1
}

/// <summary>Определяет итог публичной операции Menu API.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuOperationStatus>))]
public enum MenuOperationStatus : byte
{
    /// <summary>Операция успешно завершена.</summary>
    [JsonStringEnumMemberName("success")]
    Success = 0,

    /// <summary>Запрос не содержит обязательных данных.</summary>
    [JsonStringEnumMemberName("invalid_request")]
    InvalidRequest = 1,

    /// <summary>Технический идентификатор не соответствует правилам контракта.</summary>
    [JsonStringEnumMemberName("invalid_identifier")]
    InvalidIdentifier = 2,

    /// <summary>Запрошенный объект не найден в active snapshot или registry.</summary>
    [JsonStringEnumMemberName("not_found")]
    NotFound = 3,

    /// <summary>Admin.Core запретил доступ либо был недоступен для protected-операции.</summary>
    [JsonStringEnumMemberName("access_denied")]
    AccessDenied = 4,

    /// <summary>Provider известен, но сейчас выгружен.</summary>
    [JsonStringEnumMemberName("provider_offline")]
    ProviderOffline = 5,

    /// <summary>Schema или Provider отклонили данные.</summary>
    [JsonStringEnumMemberName("validation_failed")]
    ValidationFailed = 6,

    /// <summary>Целевой сервер не поддерживает требуемую возможность.</summary>
    [JsonStringEnumMemberName("unsupported")]
    Unsupported = 7,

    /// <summary>Регистрация или alias конфликтует с уже активным объектом.</summary>
    [JsonStringEnumMemberName("conflict")]
    Conflict = 8,

    /// <summary>Provider handler завершился ошибкой.</summary>
    [JsonStringEnumMemberName("handler_failed")]
    HandlerFailed = 9,

    /// <summary>Handle регистрации уже выгружен.</summary>
    [JsonStringEnumMemberName("disposed")]
    Disposed = 10
}

/// <summary>Определяет важность результата проверки контракта.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MenuValidationSeverity>))]
public enum MenuValidationSeverity : byte
{
    /// <summary>Информационное сообщение.</summary>
    [JsonStringEnumMemberName("info")]
    Info = 0,

    /// <summary>Предупреждение, не блокирующее публикацию само по себе.</summary>
    [JsonStringEnumMemberName("warning")]
    Warning = 1,

    /// <summary>Ошибка, блокирующая регистрацию, публикацию или активацию.</summary>
    [JsonStringEnumMemberName("error")]
    Error = 2
}
