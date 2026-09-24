using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;

namespace Common.Di.Diagnostics;

/// <summary>
/// Измеряет синхронные этапы подключения при ELYSIUM_CONNECT_DIAGNOSTICS=1.
/// Переменная читается один раз; для изменения режима требуется перезапуск сервера.
/// </summary>
public static class ConnectionDiagnostics
{
    private static readonly bool Enabled =
        Environment.GetEnvironmentVariable("ELYSIUM_CONNECT_DIAGNOSTICS") == "1";

    /// <summary>
    /// Начинает замер этапа. В обычном режиме возвращает null без создания замера.
    /// Идентификатор сессии и номер попытки позволяют отличать отложенную инициализацию от входа.
    /// </summary>
    public static Timing? Begin(
        ILogger logger, string stage, int playerId = -1, ulong sessionId = 0, int attempt = -1)
    {
        return Enabled ? new Timing(logger, stage, playerId, sessionId, attempt) : null;
    }

    /// <summary>
    /// Одноразовый замер до выхода из синхронного обработчика, включая выход по исключению.
    /// Хранит только скалярные идентификаторы, не удерживая игрока после завершения события.
    /// </summary>
    public sealed class Timing : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _stage;
        private int _playerId;
        private readonly int _attempt;
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        private readonly long _started = Stopwatch.GetTimestamp();
        private ulong _sessionId;
        private ulong _steamId;
        private int _disposed;

        internal Timing(ILogger logger, string stage, int playerId, ulong sessionId, int attempt)
        {
            _logger = logger;
            _stage = stage;
            _playerId = playerId;
            _sessionId = sessionId;
            _attempt = attempt;
        }

        /// <summary>
        /// Сохраняет идентификаторы уже полученного игрока для сопоставления с операциями БД.
        /// Недоступные сведения об игроке не должны изменять выполнение обработчика.
        /// </summary>
        public void Identify(IPlayer? player)
        {
            if (player is null) return;

            try
            {
                _playerId = player.PlayerID;
                _sessionId = player.SessionId;
                if (player.IsValid) _steamId = player.SteamID;
            }
            catch
            {
                // Диагностика не должна создавать дополнительный отказ при отключении игрока.
            }
        }

        /// <summary>
        /// Записывает длительность один раз. Ошибка диагностического логгера не заменяет
        /// результат или исходное исключение игрового обработчика.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            var elapsed = Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
            try
            {
                _logger.LogInformation(
                    "[ConnectDiag] stage={Stage} player_id={PlayerId} session_id={SessionId} " +
                    "steam_id={SteamId} attempt={Attempt} start_tick={StartTick} " +
                    "elapsed_ms={ElapsedMs:F3} thread={ThreadId}",
                    _stage, _playerId, _sessionId, _steamId, _attempt, _started, elapsed, _threadId);
            }
            catch
            {
                // Сбой вывода замера не должен влиять на подключение.
            }
        }
    }
}
