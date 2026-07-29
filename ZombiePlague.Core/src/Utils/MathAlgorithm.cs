using Common.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Di;

namespace ZombiePlague.Core.Utils;

internal static class MathAlgorithm
{
    public static Vector ForwardFromAngles(QAngle angles)
    {
        const float deg2Rad = MathF.PI / 180f;

        var pitch = angles.Pitch * deg2Rad;
        var yaw = angles.Yaw * deg2Rad;

        var cosPitch = MathF.Cos(pitch);

        return new Vector(
            cosPitch * MathF.Cos(yaw),
            cosPitch * MathF.Sin(yaw),
            -MathF.Sin(pitch)
        );
    }

    public static List<IPlayer> FindAllPlayersInSphere(float radius, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var players = core.PlayerManager.GetAlive();

        List<IPlayer> foundPlayers = [];

        foreach (IPlayer player in players)
        {
            var playerPosition = player.RequiredPawn.AbsOrigin!.Value;

            if (
                Math.Sqrt(Math.Pow(playerPosition.X - position.X, 2) +
                Math.Pow(playerPosition.Y - position.Y, 2) +
                Math.Pow(playerPosition.Z - position.Z, 2)) <= radius
            ) {
                foundPlayers.Add(player);
            }
        }

        return foundPlayers;
    }
}