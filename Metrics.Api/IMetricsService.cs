namespace Metrics.Api;

/// <summary>
/// Неблокирующая точка входа для отправки игровых событий в Elysium Metrics.
/// Реализации не должны выполнять сетевые операции или операции с диском
/// в вызывающем потоке.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Получает ключ, по которому реализация интерфейса регистрируется
    /// в общем хранилище API SwiftlyS2.
    /// </summary>
    public static readonly string SharedApiKey = "Metrics.Api.IMetricsService";

    /// <summary>
    /// Ставит событие в очередь для фоновой отправки.
    /// </summary>
    /// <param name="eventName">
    /// Ключ события, настроенный во Flute, например <c>class_selected</c>.
    /// </param>
    /// <param name="steamId">
    /// Необязательный SteamID64 игрока, связанного с событием.
    /// </param>
    /// <param name="properties">
    /// Анонимный объект или DTO, соответствующий настроенной схеме события.
    /// </param>
    public void Track(string eventName, ulong? steamId = null, object? properties = null);
}
