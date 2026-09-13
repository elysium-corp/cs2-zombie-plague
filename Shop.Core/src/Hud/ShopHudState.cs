using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

// Состояние привязано к подключению, а не только к переиспользуемому слоту игрока.
internal sealed class ShopHudState
{
    private readonly Dictionary<int, (ulong SessionId, Func<bool> IsOpen)> _open = [];
    public bool IsOpen(IPlayer player) => _open.TryGetValue(player.PlayerID, out var session)
        && session.SessionId == player.SessionId && session.IsOpen();
    public void Open(IPlayer player) => Track(player, () => true);
    // Подготовленная сущность ещё не означает открытый магазин. При проверке E
    // читаем актуальное состояние, не дожидаясь обновления захвата мыши.
    public void Track(IPlayer player, Func<bool> isOpen) => _open[player.PlayerID] = (player.SessionId, isOpen);
    public void Close(int playerId) => _open.Remove(playerId);
    public void Clear() => _open.Clear();
}
