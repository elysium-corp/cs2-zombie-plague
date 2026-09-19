using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal sealed class DamageMovementRestore(ISwiftlyCore core, IPlayerManager playerManager)
{
    private readonly Dictionary<ulong, PendingRestore> _pending = [];

    public void Schedule(IPlayer? player)
    {
        if (player is not { IsValid: true, IsAlive: true } ||
            player.PlayerPawn is not { IsValid: true } pawn ||
            !playerManager.TryGetZombie(player, out var zombie))
        {
            return;
        }

        var sessionId = player.SessionId;
        if (_pending.TryGetValue(sessionId, out var pending) &&
            pending.PawnAddress == pawn.Address && ReferenceEquals(pending.Zombie, zombie))
        {
            return;
        }

        // Несколько попаданий за один тик требуют только одного восстановления.
        var restore = new PendingRestore(pawn.Address, zombie);
        _pending[sessionId] = restore;
        core.Scheduler.NextWorldUpdate(() => RestoreScheduled(sessionId, restore));
    }

    public void Restore(IPlayer? player)
    {
        if (player is not { IsValid: true, IsAlive: true } ||
            !playerManager.TryGetZombie(player, out var zombie))
        {
            return;
        }

        player.SetSpeed(zombie.ZClass.Speed);
        player.SetGravity(zombie.ZClass.Gravity);
    }

    public void Clear() => _pending.Clear();

    private void RestoreScheduled(ulong sessionId, PendingRestore restore)
    {
        if (!_pending.TryGetValue(sessionId, out var pending) || !ReferenceEquals(pending, restore))
        {
            return;
        }

        _pending.Remove(sessionId);
        var player = core.PlayerManager.GetPlayerFromSessionId(sessionId);
        if (player is not { IsValid: true, IsAlive: true } ||
            player.PlayerPawn is not { IsValid: true } pawn ||
            pawn.Address != restore.PawnAddress ||
            !playerManager.TryGetZombie(player, out var zombie) ||
            !ReferenceEquals(zombie, restore.Zombie))
        {
            return;
        }

        Restore(player);
    }

    private sealed record PendingRestore(nint PawnAddress, IZombie Zombie);
}
