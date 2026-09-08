using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

// Состояние привязано к подключению, а не только к переиспользуемому слоту игрока.
internal sealed class ShopHudState
{
    private readonly Dictionary<int, ulong> _open = [];
    public bool IsOpen(IPlayer player) => _open.TryGetValue(player.PlayerID, out var session) && session == player.SessionId;
    public void Open(IPlayer player) => _open[player.PlayerID] = player.SessionId;
    public void Close(int playerId) => _open.Remove(playerId);
    public void Clear() => _open.Clear();
}
