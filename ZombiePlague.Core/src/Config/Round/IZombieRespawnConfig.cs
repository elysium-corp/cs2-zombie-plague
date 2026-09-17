namespace ZombiePlague.Core.Config.Round;

/// <summary>
/// Общая конфигурация автоматического возрождения зомби для режимов,
/// которые поддерживают respawn во время активного раунда.
/// </summary>
public interface IZombieRespawnConfig
{
    /// <summary>
    /// Разрешено ли автоматическое возрождение зомби после смерти.
    /// </summary>
    bool ZombieRevived { get; set; }

    /// <summary>
    /// Максимальное количество успешных автоматических возрождений одного игрока
    /// за текущий игровой раунд. Счётчик хранится по SteamID и не сбрасывается при reconnect.
    /// </summary>
    int ZombieRespawnLimit { get; set; }

    /// <summary>
    /// Задержка в секундах перед автоматическим возрождением.
    /// </summary>
    float ZombieSpawnTime { get; set; }
}
