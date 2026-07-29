using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Store.Contracts;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Store;

internal sealed class PlayerStore(IKeyValueStore keyValueStore) : IPlayerStore
{
    public void Set(IPlayer player, PlayerPreferences preferences)
    {
        var key = GetKey(player);
        keyValueStore.Set(key, preferences);
    }

    public bool TryGet(IPlayer player, [NotNullWhen(true)] out PlayerPreferences? preferences)
    {
        var key = GetKey(player);
        return keyValueStore.TryGet(key, out preferences);
    }

    public bool Remove(IPlayer player)
    {
        var key = GetKey(player);
        return keyValueStore.Remove(key);
    }

    private static string GetKey(IPlayer player)
    {
        return $"player-preferences:{player.SteamID}";
    }
}