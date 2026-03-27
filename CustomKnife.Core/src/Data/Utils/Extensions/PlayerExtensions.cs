using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Utils.Extensions;

internal static class PlayerExtensions
{
    extension(IPlayer player)
    {
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
    }
    
}