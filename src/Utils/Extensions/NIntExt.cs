using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Utils.Extensions;

public static class NIntExt
{
    extension(nint address)
    {
        public IPlayer? FindPlayerByPawnAddress()
        {
            var core = DependencyManager.GetService<ISwiftlyCore>();
            
            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                var pawnAddress = player.RequiredPlayerPawn.Address;

                if (pawnAddress == address) return player;
            }

            return null;
        }
    }
}