using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Extensions;

public static class PlayerExtensions
{
    public static void SetHealth(this IPlayer player, int health)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null || playerPawn.Health <= 0) return;

        playerPawn.MaxHealth = health;
        playerPawn.MaxHealthUpdated();
        playerPawn.Health = health;
        playerPawn.HealthUpdated();
    }

    public static void SetArmor(this IPlayer player, int armor)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null || !player.Controller.PawnIsAlive) return;

        if (armor <= 0)
        {
            playerPawn.ArmorValue = 0;
        }
        else
        {
            playerPawn.ArmorValue = armor;
        }

        playerPawn.ArmorValueUpdated();
    }

    // Стандартное значение скорости 250
    public static void SetSpeed(this IPlayer player, float speed)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null || !player.Controller.PawnIsAlive) return;

        playerPawn.VelocityModifier = speed / 250;
        playerPawn.VelocityModifierUpdated();
    }

    // Стандартное значение гравитации 800
    public static void SetGravity(this IPlayer player, float gravity)
    {
        var pawn = player.Pawn;
        if (pawn == null || !player.Controller.PawnIsAlive) return;

        pawn.GravityScale = gravity / 800;
        pawn.ActualGravityScale = gravity / 800;
        pawn.GravityScaleUpdated();
    }

    public static void SetModel(this IPlayer player, string modelPath)
    {
        if (player.PlayerPawn == null || !player.Controller.PawnIsAlive) return;
        
        DependencyManager.GetService<ISwiftlyCore>().Scheduler.NextTick(() =>
        {
            if (player.IsValid)
            {
                player.RequiredPawn.SetModel(modelPath);
            }
        });
    }
    public static bool IsInfected(this IPlayer player)
    {
        var allZombies = DependencyManager.GetService<ZombieManager>().GetAllZombies();
        return allZombies.ContainsKey(player.PlayerID);
    }

    public static bool IsNemesis(this IPlayer player)
    {
        if (player.IsInfected())
        {
            var zombie = DependencyManager.GetService<ZombieManager>().GetZombie(player.PlayerID);
            return zombie.IsNemesis;
        }

        return false;
    }

    public static bool IsLastHuman(this IPlayer player)
    {
        return !player.IsInfected() && DependencyManager.GetService<HumanManager>().GetCountHumans() == 1;
    }

    public static bool IsFrozen(this IPlayer player)
    {
        return player.PlayerPawn != null && (player.PlayerPawn.MoveType == MoveType_t.MOVETYPE_FLY ? true : false);
    }
}