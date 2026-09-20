using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Data;

internal readonly record struct PlayerActionTarget(int PlayerId, ulong SessionId)
{
    public static PlayerActionTarget From(IPlayer player) => new(player.PlayerID, player.SessionId);

    public IPlayer? Resolve(ISwiftlyCore core)
    {
        var player = core.PlayerManager.GetPlayer(PlayerId);
        return player is { IsValid: true } && player.SessionId == SessionId ? player : null;
    }
}
