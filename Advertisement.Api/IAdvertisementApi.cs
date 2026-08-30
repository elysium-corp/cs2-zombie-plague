using SwiftlyS2.Shared.Players;

namespace Advertisement.Api;

/// <summary>
/// Предоставляет публичный API локализованных сообщений Advertisement.Core.
/// </summary>
/// <remarks>
/// Все методы работают с текущим snapshot в памяти и не выполняют запросы
/// к PostgreSQL в игровом потоке.
/// </remarks>
public interface IAdvertisementApi
{
    /// <summary>
    /// Возвращает эффективный язык игрока, определённый общим Localization.Core.
    /// </summary>
    /// <param name="player">Подключённый игрок.</param>
    /// <returns>Нормализованный код включённого языка.</returns>
    [Obsolete("Используйте ILocalizationApi.Resolve(IPlayer). Метод сохранён для совместимости.")]
    string GetPlayerLocale(IPlayer player);

    /// <summary>
    /// Возвращает текст сообщения для указанной локали.
    /// </summary>
    /// <param name="messageKey">Уникальный ключ сообщения.</param>
    /// <param name="locale">Код локали.</param>
    /// <returns>Текст с разметкой либо <c>null</c>, если сообщение или перевод не найдены.</returns>
    string? GetText(string messageKey, string locale);

    /// <summary>
    /// Возвращает текст сообщения для эффективной локали игрока.
    /// </summary>
    /// <param name="messageKey">Уникальный ключ сообщения.</param>
    /// <param name="player">Подключённый игрок.</param>
    /// <returns>Текст с разметкой либо <c>null</c>, если сообщение или перевод не найдены.</returns>
    string? GetText(string messageKey, IPlayer player);

    /// <summary>
    /// Отправляет игроку локализованное сообщение из текущего snapshot.
    /// </summary>
    /// <param name="player">Получатель.</param>
    /// <param name="messageKey">Уникальный ключ сообщения.</param>
    /// <param name="tagKey">Ключ тега, переопределяющего тег сообщения, или <c>null</c>.</param>
    /// <returns><c>true</c>, если сообщение и переданный тег найдены и отправка запущена.</returns>
    bool Send(IPlayer player, string messageKey, string? tagKey = null);

    /// <summary>
    /// Отправляет локализованное сообщение всем авторизованным игрокам.
    /// </summary>
    /// <param name="messageKey">Уникальный ключ сообщения.</param>
    /// <param name="tagKey">Ключ тега, переопределяющего тег сообщения, или <c>null</c>.</param>
    /// <returns>Количество выбранных получателей или <c>0</c>, если сообщение либо тег не найдены.</returns>
    int SendToAll(string messageKey, string? tagKey = null);

    /// <summary>
    /// Ключ Shared Interface.
    /// </summary>
    public static readonly string SharedApiKey = "Advertisement.Api.IAdvertisementApi";
}
