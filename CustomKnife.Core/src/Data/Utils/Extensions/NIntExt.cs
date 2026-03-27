using Common.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Utils.Extensions;

internal static class NIntExt
{
    extension(nint address)
    {
        public IPlayer? FindPlayerByPawnAddress()
        {
            var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
            
            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                var pawnAddress = player.RequiredPlayerPawn.Address;

                if (pawnAddress == address) return player;
            }

            return null;
        }
    }
}