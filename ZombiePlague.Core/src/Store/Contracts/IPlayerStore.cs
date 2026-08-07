using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Store.Contracts;

internal interface IPlayerStore
{
    void Set(IPlayer player, PlayerPreferences preferences);

    bool TryGet(IPlayer player, [NotNullWhen(true)] out PlayerPreferences? preferences);

    bool Remove(IPlayer player);
}