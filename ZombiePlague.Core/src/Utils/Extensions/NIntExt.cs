using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Di;

namespace ZombiePlague.Core.Utils.Extensions;

internal static class NIntExt
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