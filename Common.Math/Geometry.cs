using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Common.Math;

public static class Geometry
{
    public static IReadOnlyList<IPlayer> FindPlayersInSphere(IEnumerable<IPlayer> players, float radius, Vector position)
    {
        var foundPlayers = new List<IPlayer>();
        var radiusSquared = radius * radius;

        foreach (var player in players)
        {
            var absOrigin = player.RequiredPawn.AbsOrigin;
            if (absOrigin == null)
                continue;

            var playerPosition = absOrigin.Value;

            var dx = playerPosition.X - position.X;
            var dy = playerPosition.Y - position.Y;
            var dz = playerPosition.Z - position.Z;

            if (dx * dx + dy * dy + dz * dz <= radiusSquared)
            {
                foundPlayers.Add(player);
            }
        }

        return foundPlayers;
    }
}