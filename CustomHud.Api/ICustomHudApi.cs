using SwiftlyS2.Shared.Players;

namespace CustomHud.Api;

/// <summary>Показывает временные сообщения через общий Custom HUD, независимо от меню и панели способностей.</summary>
/// <remarks>
/// Все методы и свойства вызываются на игровом потоке SwiftlyS2
/// Из фоновой задачи сначала перейдите на него через Core.Scheduler.NextWorldUpdate
/// Доставка клиенту не подтверждается: true означает принятие сообщения сервером
/// </remarks>
public interface ICustomHudApi
{
    /// <summary>Ключ общего интерфейса SwiftlyS2.</summary>
    public const string SharedApiKey = "CustomHud.Api.ICustomHudApi";

    /// <summary>Готова ли серверная сущность к приёму сообщений; наличие VPK у клиента не проверяется.</summary>
    bool IsAvailable { get; }

    /// <summary>Показывает текст одному подключённому игроку, заменяя его сообщение с тем же каналом и позицией.</summary>
    /// <param name="player">Получатель; боты и отключённые игроки пропускаются.</param>
    /// <param name="text">Обычный текст или поддерживаемая разметка, не более 4096 символов.</param>
    /// <param name="options">Положение, срок показа, оформление и канал; null использует значения по умолчанию.</param>
    /// <returns>Сообщение принято; false при недоступности HUD, пустом тексте или заполнении лимита сообщений игрока.</returns>
    /// <exception cref="ArgumentException">Переданы некорректные параметры оформления или слишком длинный текст.</exception>
    bool Show(IPlayer player, string text, HudMessageOptions? options = null);

    /// <summary>Отправляет одинаковый текст всем подключённым игрокам, кроме ботов; поздние подключения его не получают.</summary>
    /// <param name="text">Текст в том же формате, что и у Show.</param>
    /// <param name="options">Параметры сообщения.</param>
    /// <returns>Количество получателей, для которых сообщение принято.</returns>
    int Broadcast(string text, HudMessageOptions? options = null);

    /// <summary>Удаляет сообщения указанного канала у игрока, включая временно перекрытые более приоритетным сообщением.</summary>
    /// <param name="player">Получатель.</param>
    /// <param name="channel">Канал, принадлежащий вызывающему плагину.</param>
    void Hide(IPlayer player, string channel);

    /// <summary>Удаляет сообщения канала у всех игроков; вызывайте при выгрузке плагина-владельца.</summary>
    /// <param name="channel">Уникальный канал плагина, например Advertisement.Periodic.</param>
    void ClearChannel(string channel);
}
