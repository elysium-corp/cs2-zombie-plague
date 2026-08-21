namespace Admin.Core.Services;

/// <summary>
/// Управляет периодической синхронизацией runtime-привилегий
/// онлайн-игроков с persistent-хранилищем.
/// </summary>
internal interface IPlayerPrivilegeRefreshService
{
    /// <summary>
    /// Запускает фоновую периодическую синхронизацию привилегий.
    /// </summary>
    void Start();

    /// <summary>
    /// Останавливает фоновую синхронизацию и ожидает завершения
    /// выполняющейся операции обновления.
    /// </summary>
    void StopAndWait();
}