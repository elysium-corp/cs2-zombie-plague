using System.Collections.Concurrent;

namespace CustomKnife.Data.Store;

internal sealed class PlayerKnifeStore
{
    private readonly ConcurrentDictionary<ulong, PlayerKnifePreferences> _players = new();

    public PlayerKnifePreferences GetOrCreate(ulong steamId, string defaultKnifeId)
    {
        return _players.GetOrAdd(steamId, _ => new PlayerKnifePreferences
            {
                KnifeId = defaultKnifeId
            }
        );
    }

    public PlayerKnifePreferences? Get(ulong steamId)
    {
        return _players.GetValueOrDefault(steamId);
    }

    public void SetKnifeId(ulong steamId, string knifeId)
    {
        _players.AddOrUpdate(steamId, _ => new PlayerKnifePreferences
            {
                KnifeId = knifeId
            },
            (_, _) => new PlayerKnifePreferences
            {
                KnifeId = knifeId
            }
        );
    }
    
    public bool TrySetKnifeId(ulong steamId, PlayerKnifePreferences expected, string knifeId)
    {
        return _players.TryUpdate(steamId, new PlayerKnifePreferences
            {
                KnifeId = knifeId
            },
            expected
        );
    }

    public void Remove(ulong steamId)
    {
        _players.TryRemove(steamId, out _);
    }
}