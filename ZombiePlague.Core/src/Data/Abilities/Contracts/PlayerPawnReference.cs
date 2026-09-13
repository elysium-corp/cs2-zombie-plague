using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

// Идентификатор сессии не позволяет попасть в повторно занятый слот,
// полный handle отличает заменённый pawn даже при повторном использовании индекса.
internal readonly record struct PlayerPawnReference(ulong SessionId, uint PawnHandle)
{
    public bool TryResolve(ISwiftlyCore core, [NotNullWhen(true)] out CCSPlayerPawn? pawn)
    {
        pawn = null;
        try
        {
            var player = core.PlayerManager.GetPlayerFromSessionId(SessionId);
            if (player is not { IsValid: true, IsAlive: true } || player.SessionId != SessionId ||
                player.PlayerPawn is not { IsValid: true } currentPawn ||
                core.EntitySystem.GetRefEHandle(currentPawn).Raw != PawnHandle) return false;

            pawn = currentPawn;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}
