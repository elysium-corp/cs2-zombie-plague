using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Utils.Extensions;

internal static class NIntExt
{
    extension(nint address)
    {
        public IPlayer? FindPlayerByPawnAddress(ISwiftlyCore core)
        {
            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                var pawnAddress = player.RequiredPlayerPawn.Address;

                if (pawnAddress == address) return player;
            }

            return null;
        }
    }
}
