using CS2ZombiePlague.Data.Lifecycle;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Extensions;

public static class PlayerExtensions
{
    extension(IPlayer player)
    {
        public void SetHealth(int health)
        {
            var playerPawn = player.PlayerPawn;
            if (playerPawn == null)
            {
                return;
            }
        
            playerPawn.Health = health;
            playerPawn.HealthUpdated();
        }

        public void SetArmor(int armor)
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
        public void SetSpeed(float speed)
        {
            var playerPawn = player.PlayerPawn;
            if (playerPawn == null || !player.Controller.PawnIsAlive) return;

            playerPawn.VelocityModifier = speed / 250;
            playerPawn.VelocityModifierUpdated();
        }

        // Стандартное значение гравитации 800
        public void SetGravity(float gravity)
        {
            var pawn = player.Pawn;
            if (pawn == null || !player.Controller.PawnIsAlive) return;

            pawn.GravityScale = gravity / 800;
            pawn.ActualGravityScale = gravity / 800;
            pawn.GravityScaleUpdated();
        }

        public void SetModel(string modelPath)
        {
            if (player.PlayerPawn == null || !player.Controller.PawnIsAlive) return;
        
            DependencyManager.GetService<ISwiftlyCore>().Scheduler.NextWorldUpdateAsync(() =>
            {
                player.Pawn.SetModel(modelPath);
            });
        }

        public bool IsInfected()
        {
            var allZombies = DependencyManager.GetService<ZombieManager>().GetAllZombies();
            return allZombies.ContainsKey(player.PlayerID);
        }

        public bool IsNemesis()
        {
            if (player.IsInfected())
            {
                var zombie = DependencyManager.GetService<ZombieManager>().GetZombie(player.PlayerID);
                return zombie.IsNemesis;
            }

            return false;
        }

        public bool IsLastHuman()
        {
            return !player.IsInfected() && DependencyManager.GetService<HumanManager>().GetCountHumans() == 1;
        }

        public bool IsFrozen()
        {
            return player.PlayerPawn != null && (player.PlayerPawn.MoveType == MoveType_t.MOVETYPE_FLY ? true : false);
        }
        
        public IPlayerLifecycle GetLifecycle()
        {
            var playerLifecycleManager = DependencyManager.GetService<PlayerLifecycleManager>();

            return playerLifecycleManager.GetPlayerWithLifecycle(player);
        }
    }
}