using Common.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Utils.Extensions;

internal static class NIntExt
{
    extension(nint address)
    {
        public IPlayer? FindPlayerByPawnAddress()
        {
            if (address == nint.Zero)
            {
                return null;
            }
            
            var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
            
            foreach (var player in core.PlayerManager.GetAllValidPlayers())
            {
                var pawn = player.PlayerPawn;

                if (pawn is not { IsValid: true })
                {
                    continue;
                }

                if (pawn.Address == address)
                {
                    return player;
                }
            }

            return null;
        }
    }
}