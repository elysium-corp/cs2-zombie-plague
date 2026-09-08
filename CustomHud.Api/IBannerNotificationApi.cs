using SwiftlyS2.Shared.Players;

namespace CustomHud.Api;

/// <summary>Игровые уведомления, оформление и доставка которых задаются в CMS. Вызовы только на игровом потоке.</summary>
public interface IBannerNotificationApi
{
    /// <summary>Ключ общего интерфейса, предоставляемого Advertisement.Core.</summary>
    public const string SharedApiKey = "CustomHud.Api.IBannerNotificationApi";
    /// <summary>Публикует событие одному игроку. false означает отключение события, ограничение повторов или недоступность HUD.</summary>
    bool Publish(IPlayer player, string eventKey, IReadOnlyDictionary<string, object?>? parameters = null);
    /// <summary>Публикует событие всем настоящим игрокам; язык и общие параметры определяются для каждого получателя.</summary>
    int Broadcast(string eventKey, IReadOnlyDictionary<string, object?>? parameters = null);
    /// <summary>Добавляет контекст плагина без зависимости общего сервиса от реализации плагина. Токен нужно освободить при выгрузке.</summary>
    IDisposable RegisterContext(string owner, Func<IPlayer, IReadOnlyDictionary<string, object?>> provider);
    /// <summary>Отменяет ожидающие и видимые экземпляры события у всех игроков.</summary>
    void Clear(string eventKey);
    /// <summary>Возвращает настройки постоянного HUD из того же снимка CMS.</summary>
    HudWidgetOptions? GetWidget(string key);
    /// <summary>Подписывает потребителя на применение нового снимка, на игровом потоке. Освободите токен при выгрузке.</summary>
    IDisposable SubscribeConfiguration(Action callback);
}

/// <summary>Настройки постоянного HUD способностей; не являются всплывающим баннером.</summary>
public sealed record HudWidgetOptions
{
    /// <summary>Разрешена ли отрисовка.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Положение: девять областей, middle_center — центр. top_left размещается под радаром.</summary>
    public string Position { get; init; } = "top_left";
    /// <summary>Масштаб: 50, 75 или 100 процентов.</summary>
    public int ScalePercent { get; init; } = 100;
    /// <summary>Показывать названия способностей.</summary>
    public bool ShowNames { get; init; } = true;
    /// <summary>Использовать сохранённые игроком расположение и масштаб.</summary>
    public bool AllowPlayerCustomization { get; init; } = true;
}

/// <summary>Перепривязываемая ссылка для DI потребителя; отсутствие поставщика не ломает игровую механику.</summary>
public sealed class BannerNotificationClient
{
    private IBannerNotificationApi? _api;
    /// <summary>Устанавливает актуальную ссылку после инъекции shared-интерфейсов либо снимает её при выгрузке.</summary>
    public void Bind(IBannerNotificationApi? api) => _api = api;
    /// <summary>Публикует событие с типизированными параметрами.</summary>
    public bool Publish(IPlayer player, string eventKey, IReadOnlyDictionary<string, object?>? parameters = null) =>
        _api?.Publish(player, eventKey, parameters) ?? false;
    /// <summary>Переходный адаптер для существующих строковых параметров Localization.</summary>
    public bool PublishText(IPlayer player, string eventKey, IReadOnlyDictionary<string, string>? parameters = null) =>
        Publish(player, eventKey, parameters?.ToDictionary(pair => pair.Key, pair => (object?)pair.Value));
    /// <summary>Отправляет событие всем игрокам.</summary>
    public int Broadcast(string eventKey, IReadOnlyDictionary<string, object?>? parameters = null) =>
        _api?.Broadcast(eventKey, parameters) ?? 0;
    /// <summary>Снимает все экземпляры события.</summary>
    public void Clear(string eventKey) => _api?.Clear(eventKey);
}

/// <summary>Снимок настройки события. Сериализуется в fallback вместе с дизайном, без запросов БД при показе.</summary>
public sealed record BannerNotificationRule
{
    /// <summary>Стабильный ключ игрового события.</summary>
    public string EventKey { get; init; } = "";
    /// <summary>Разрешён ли показ события.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Дизайн из конструктора.</summary>
    public HudBannerTemplate Template { get; init; } = new() { Variant = "text" };
    /// <summary>Ключи Localization включённых блоков.</summary>
    public HudBannerContent Content { get; init; } = new();
    /// <summary>Позиция, приоритет и длительность баннера.</summary>
    public HudMessageOptions Options { get; init; } = new();
    /// <summary>replace обновляет текущий баннер; queue сохраняет последовательность сообщений в области.</summary>
    public string Delivery { get; init; } = "queue";
    /// <summary>Минимальный интервал между принятыми событиями одного типа у игрока: 0–300 секунд.</summary>
    public double CooldownSeconds { get; init; }
    /// <summary>Предел ожидания в очереди: 1–120 секунд.</summary>
    public double MaxQueueAgeSeconds { get; init; } = 30;
    /// <summary>Получатель: all, alive, dead, humans, zombies, spectators.</summary>
    public string Audience { get; init; } = "all";
    /// <summary>Нижняя граница количества настоящих игроков.</summary>
    public int MinPlayers { get; init; }
    /// <summary>Пользовательские значения; автоматические параметры события имеют приоритет.</summary>
    public Dictionary<string, object?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
