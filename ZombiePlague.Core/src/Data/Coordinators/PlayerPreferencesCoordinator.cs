using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Coordinators.Contracts;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Store.Contracts;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Data.Coordinators;

internal sealed class PlayerPreferencesCoordinator(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IPlayerStore playerStore,
    IPlayerPersistenceService playerPersistenceService
) : IPlayerPreferencesCoordinator
{
    // - активные операции с бд
    private readonly object _databaseTasksLock = new();
    private readonly HashSet<Task> _databaseTasks = [];
    // - игроки для которых уже был получен результат из бд
    private readonly HashSet<ulong> _playersReadyForSave = [];

    // - если плагин будет выключен, чтобы избежать модификаций PlayerStore
    private volatile bool _canApplyLoadedPreferences = true;

    public void Initialize(IPlayer player)
    {
        if (!CanInitialize(player))
        {
            return;
        }

        var defaults = new PlayerPreferences();
        var steamId = player.SteamID;

        // - не ждем бд, записываем сразу же в стор
        playerStore.Set(player, defaults);
        // - запрещаем для новой сессии игрока сохранение
        _playersReadyForSave.Remove(steamId);

        // - запускаем асинхронную загрузку 
        RunDatabaseTask(LoadAsync(steamId, defaults));
    }

    public void SaveAndRemove(IPlayer player)
    {
        var steamId = player.SteamID;

        var isReadyForSave = _playersReadyForSave.Remove(steamId);

        if (CanSave(player) && isReadyForSave && playerStore.TryGet(player, out var preferences))
        {
            RunDatabaseTask(SaveAsync(steamId, preferences));
        }

        playerStore.Remove(player);
    }

    public void SaveAllAndWait()
    {
        _canApplyLoadedPreferences = false;

        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            SaveAndRemove(player);
        }

        WaitForDatabaseTasks();
        _playersReadyForSave.Clear();
    }

    private async Task LoadAsync(ulong steamId, PlayerPreferences defaults)
    {
        try
        {
            var loaded = await playerPersistenceService
                .LoadAsync(steamId)
                .ConfigureAwait(false) ?? defaults;

            if (!_canApplyLoadedPreferences)
            {
                return;
            }

            // - переключаемся на игровой поток для внесения изменений
            core.Scheduler.NextWorldUpdate(() =>
            {
                ApplyLoadedPreferences(steamId, defaults, loaded);
            });
        }
        catch (Exception exception)
        {
            core.Logger.LogError(exception,
                "Failed to load preferences for SteamID {SteamId} from Database. " +
                "Defaults will remain in memory and will not be saved until the database is read successfully!",
                steamId
            );
        }
    }

    private void ApplyLoadedPreferences(ulong steamId, PlayerPreferences defaults, PlayerPreferences loaded)
    {
        if (!_canApplyLoadedPreferences)
        {
            return;
        }

        // - нужно получить актуального игрока
        var player = core.PlayerManager.GetPlayerFromSteamId(steamId, false);

        if (
            player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false } || 
            !playerStore.TryGet(player, out var current))
        {
            return;
        }

        var merged = loaded with
        {
            ZClassId = current.ZClassId == defaults.ZClassId
                ? loaded.ZClassId
                : current.ZClassId,

            HClassId = current.HClassId == defaults.HClassId
                ? loaded.HClassId
                : current.HClassId,

            KnifeId = current.KnifeId
        };

        playerStore.Set(player, merged);
        
        // - теперь при выходе данные игрока можно перезаписать
        _playersReadyForSave.Add(steamId);

        if (!player.IsAlive && playerManager.IsHuman(player))
        {
            playerManager.TrySetHuman(player);
        }
    }

    private async Task SaveAsync(ulong steamId, PlayerPreferences preferences)
    {
        try
        {
            await playerPersistenceService
                .SaveAsync(steamId, preferences)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Failed to save preferences for SteamID {SteamId} " +
                "after all database retries. Session changes were lost.",
                steamId
            );
        }
    }

    private void RunDatabaseTask(Task task)
    {
        lock (_databaseTasksLock)
        {
            _databaseTasks.Add(task);
        }

        _ = RemoveCompletedTaskAsync(task);
    }

    private async Task RemoveCompletedTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            core.Logger.LogError(exception, "Unhandled player database operation error.");
        }
        finally
        {
            lock (_databaseTasksLock)
            {
                _databaseTasks.Remove(task);
            }
        }
    }

    private void WaitForDatabaseTasks()
    {
        Task[] tasks;

        lock (_databaseTasksLock)
        {
            tasks = [.. _databaseTasks];
        }

        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    private static bool CanInitialize(IPlayer player)
    {
        return player is
        {
            IsValid: true,
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }

    private static bool CanSave(IPlayer player)
    {
        return player is
        {
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }
}
