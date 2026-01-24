using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Vector = SwiftlyS2.Shared.Natives.Vector;

namespace CS2ZombiePlague.Data;

public class CommonUtils(ISwiftlyCore core)
{
    private readonly Random _randomizer = new();
    public IPlayer? ResolvePlayerFromHandle(CHandle<CEntityInstance> handle)
    {
        if (!handle.IsValid)
            return null;

        var entity = handle.Value;
        if (entity == null)
            return null;

        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            try
            {
                if (player.PlayerPawn?.Address == entity.Address ||
                    player.Controller?.Address == entity.Address)
                    return player;
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }

        return null;
    }

    public IPlayer? FindPlayerByPawnAddress(nint address)
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            try
            {
                var pawn = player.PlayerPawn;
                if (pawn != null && pawn.Address == address)
                    return player;
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Сбрасывает цвет визуализации (Render color) у всех валидных игроков
    /// до стандартного белого значения (255, 255, 255).
    /// </summary>
    public void AllResetRenderColor()
    {
        var players = core.PlayerManager.GetAllValidPlayers();
        
        foreach (var player in players)
        {
            player.RequiredPlayerPawn.Render = new Color(255, 255, 255);
            player.RequiredPlayerPawn.AnimatedEveryTickUpdated();
        }
    }

    public void MoveAllPlayersToTeam(Team team)
    {
        var players = core.PlayerManager.GetAllValidPlayers();
        
        foreach (var player in players)
        {
            player.SwitchTeam(team);
        }
    }
    
    public List<IPlayer> FindAllPlayersInSphere(float radius, Vector position)
    {
        var allPlayers = core.PlayerManager.GetAllValidPlayers();
        List<IPlayer> foundPlayers = new();
        
        foreach (IPlayer player in allPlayers)
        {
            if (player.IsAlive)
            {
                var playerPosition = player.RequiredPawn.AbsOrigin!.Value;
                if (Math.Sqrt(Math.Pow(playerPosition.X - position.X, 2) + Math.Pow(playerPosition.Y - position.Y, 2) +
                              Math.Pow(playerPosition.Z - position.Z, 2)) <= radius)
                {
                    foundPlayers.Add(player);
                }
            }
        }
        return foundPlayers;
    }
    
    public Vector ForwardFromAngles(QAngle angles)
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

    public int RandomNum(int num1, int num2)
    {
        return _randomizer.Next(num1, num2);
    }
}