using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Utils.Extensions;

public static class CHandleExt
{
    extension(CHandle<CEntityInstance> handle)
    {
        public IPlayer? ResolvePlayerFromHandle()
        {
            if (!handle.IsValid) return null;

            var address = handle.Value?.Address;
            
            if (address == null) return null;

            var core = DependencyManager.GetService<ISwiftlyCore>();

            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                if (player.PlayerPawn?.Address == address || player.Controller.Address == address)
                {
                    return player;
                }
            }

            return null;
        }
    }
}