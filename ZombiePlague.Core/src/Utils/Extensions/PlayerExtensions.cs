using Common.Di;
using Common.Effects;
using Common.Effects.Effects;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Di;

namespace ZombiePlague.Core.Utils.Extensions;

internal static class PlayerExtensions
{
    extension(IPlayer player)
    {
        public void SetHealth(int health)
        {
            var playerPawn = player.PlayerPawn;
            if (playerPawn == null || !player.IsAlive)
            {
                return;
            }
        
            playerPawn.Health = health <= 0 ? 0 : health;
            playerPawn.MaxHealth = health <= 0 ? 0 : health;
            playerPawn.HealthUpdated();
            playerPawn.MaxHealthUpdated();
        }

        public void SetArmor(int armor)
        {
            var playerPawn = player.PlayerPawn;
            if (playerPawn == null || !player.IsAlive)
            {
                return;
            }
            
            playerPawn.ArmorValue = armor <= 0 ? 0 : armor;
            playerPawn.ArmorValueUpdated();
        }

        // Стандартное значение скорости 250
        public void SetSpeed(float speed)
        {
            var playerPawn = player.PlayerPawn;
            if (playerPawn == null || !player.IsAlive)
            {
                return;
            }

            playerPawn.VelocityModifier = speed / 250;
            playerPawn.VelocityModifierUpdated();
        }

        // Стандартное значение гравитации 800
        public void SetGravity(float gravity)
        {
            var pawn = player.Pawn;
            if (pawn == null || !player.IsAlive)
            {
                return;
            }

            pawn.GravityScale = gravity / 800;
            pawn.ActualGravityScale = gravity / 800;
            pawn.GravityScaleUpdated();
        }

        public void SetModel(string modelPath)
        {
            if (player.PlayerPawn == null || !player.IsAlive)
            {
                return;
            }
        
            DependencyManager.GetService<ISwiftlyCore>().Scheduler.NextWorldUpdateAsync(() =>
            {
                player.Pawn?.SetModel(modelPath);
            });
        }

        public bool IsInfected()
        {
            var allZombies = DependencyManager.GetService<ZombieManager>().GetAllZombies();
            return allZombies.ContainsKey(player.PlayerID);
        }

        public bool IsNemesis()
        {
            var zombie = DependencyManager.GetService<ZombieManager>().GetZombie(player.PlayerID);
            return zombie?.IsNemesis ?? false;
    }
        
        public bool IsHuman()
        {
            var allHumans = DependencyManager.GetService<HumanManager>().GetAllHumanPlayers();
            return allHumans.Contains(player);
        }

        public bool IsLastHuman()
        {
            var humanCount = DependencyManager.GetService<HumanManager>().GetHumanCount();
            return player.IsHuman() && humanCount == 1;
        }

        public bool IsSurvivor()
        {
            var human = DependencyManager.GetService<HumanManager>().GetHuman(player);
            return human?.IsSurvivor ?? false;
        }

        public bool IsFrozen()
        {
            var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
            var effectService = EffectService.Provide(core);

            return effectService.HasEffect<Freeze>(player);
        }
    }
}