using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Respawn;

/// <summary>
/// Хранит количество автоматических возрождений в пределах одного экземпляра раунда.
/// Ключом служит SteamID, поэтому reconnect в том же раунде не сбрасывает лимит.
/// Новый игровой раунд получает новый tracker и начинает с чистого состояния.
/// </summary>
internal sealed class ZombieRespawnTracker(IZombieRespawnConfig config)
{
    private readonly Dictionary<ulong, RespawnState> _states = [];

    public bool TryQueueRespawn(ulong steamId, DateTimeOffset now)
    {
        var state = GetOrCreate(steamId);
        state.PendingRespawn = false;
        state.RespawnAt = null;

        if (!config.ZombieRevived ||
            config.ZombieRespawnLimit <= 0 ||
            state.UsedRespawns >= config.ZombieRespawnLimit)
        {
            state.Eliminated = true;
            return false;
        }

        state.Eliminated = false;
        state.PendingRespawn = true;
        state.RespawnAt = now.AddSeconds(Math.Max(0.0f, config.ZombieSpawnTime));
        return true;
    }

    public bool IsPending(ulong steamId)
    {
        return _states.TryGetValue(steamId, out var state) && state.PendingRespawn;
    }

    public bool IsEliminated(ulong steamId)
    {
        return _states.TryGetValue(steamId, out var state) && state.Eliminated;
    }

    public TimeSpan GetRemainingDelay(ulong steamId, DateTimeOffset now)
    {
        if (!_states.TryGetValue(steamId, out var state) ||
            !state.PendingRespawn ||
            state.RespawnAt is null)
        {
            return TimeSpan.Zero;
        }

        var remaining = state.RespawnAt.Value - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public bool CompleteAutomaticRespawn(ulong steamId)
    {
        if (!_states.TryGetValue(steamId, out var state) || !state.PendingRespawn)
        {
            return false;
        }

        state.UsedRespawns++;
        state.PendingRespawn = false;
        state.Eliminated = false;
        state.RespawnAt = null;
        return true;
    }

    public void CompleteExternalRespawn(ulong steamId)
    {
        if (!_states.TryGetValue(steamId, out var state) || !state.PendingRespawn)
        {
            return;
        }

        // Админский/внешний respawn не расходует автоматическую жизнь.
        state.PendingRespawn = false;
        state.Eliminated = false;
        state.RespawnAt = null;
    }

    public void FailPendingRespawn(ulong steamId)
    {
        if (!_states.TryGetValue(steamId, out var state) || !state.PendingRespawn)
        {
            return;
        }

        state.PendingRespawn = false;
        state.Eliminated = true;
        state.RespawnAt = null;
    }

    public IEnumerable<ulong> GetPendingSteamIds()
    {
        return _states
            .Where(static pair => pair.Value.PendingRespawn)
            .Select(static pair => pair.Key);
    }

    internal int GetUsedRespawns(ulong steamId)
    {
        return _states.TryGetValue(steamId, out var state) ? state.UsedRespawns : 0;
    }

    public void Clear()
    {
        _states.Clear();
    }

    private RespawnState GetOrCreate(ulong steamId)
    {
        if (_states.TryGetValue(steamId, out var state))
        {
            return state;
        }

        state = new RespawnState();
        _states[steamId] = state;
        return state;
    }

    private sealed class RespawnState
    {
        public int UsedRespawns { get; set; }
        public bool PendingRespawn { get; set; }
        public bool Eliminated { get; set; }
        public DateTimeOffset? RespawnAt { get; set; }
    }
}
