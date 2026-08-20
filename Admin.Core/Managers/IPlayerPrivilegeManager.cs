using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

/// <summary>
/// Управляет runtime-состоянием привилегий подключённых игроков.
/// </summary>
internal interface IPlayerPrivilegeManager
{
    /// <summary>
    /// Создаёт runtime-сессию игрока и запускает загрузку
    /// его активных назначений из persistent-хранилища.
    /// </summary>
    void Initialize(IPlayer player);

    /// <summary>
    /// Удаляет runtime-состояние отключившегося игрока.
    /// </summary>
    void Remove(IPlayer player);

    /// <summary>
    /// Перезагружает назначения указанного онлайн-игрока.
    /// </summary>
    Task<bool> ReloadAsync(ulong steamId);

    /// <summary>
    /// Перезагружает назначения всех онлайн-игроков.
    /// </summary>
    Task ReloadAllAsync();

    /// <summary>
    /// Останавливает новые операции БД
    /// и ожидает завершения уже запущенных.
    /// </summary>
    void StopAndWait();
}