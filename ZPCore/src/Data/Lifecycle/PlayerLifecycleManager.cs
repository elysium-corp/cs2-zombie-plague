using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Lifecycle;

internal sealed class PlayerLifecycleManager : ILifecycle
{
    private readonly Dictionary<ulong, IPlayerLifecycle> _players = [];

    public bool Add(IPlayerLifecycle player)
    {
        player.Bind();
        return _players.TryAdd(player.Player.SteamID, player);
    }

    public void Remove(IPlayerLifecycle player)
    {
        player.Dispose();
        _players.Remove(player.Player.SteamID);
    }

    public void RemoveAll()
    {
        foreach (var player in _players.Values)
        {
            player.Dispose();
        }
        _players.Clear();
    }
    
    public void Dispose()
    {
        foreach (var player in _players.Values)
        {
            player.Dispose();
        }
    }

    public IPlayerLifecycle GetPlayerWithLifecycle(IPlayer player)
    {
        var steamId = player.SteamID;

        if (_players.TryGetValue(steamId, out var existing))
        {
            return existing;
        }

        var lifecycle = new PlayerLifecycle(player);

        _players.Add(steamId, lifecycle);
        return lifecycle;
    }

    public List<IPlayerLifecycle> GetPlayers()
    {
        return _players.Values.ToList();
    }
}