using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal sealed class DamageMovementRestore(ISwiftlyCore core, IPlayerManager playerManager)
{
    private readonly Dictionary<ulong, PendingRestore> _pending = [];

    public void Capture(IPlayer? player)
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

        // Несколько попаданий за один тик не должны сохранять уже замедленную скорость.
        var restore = new PendingRestore(pawn.Address, zombie, pawn.VelocityModifier * 250f);
        _pending[sessionId] = restore;
        core.Scheduler.NextWorldUpdate(() => Restore(sessionId, restore));
    }

    public void Clear() => _pending.Clear();

    private void Restore(ulong sessionId, PendingRestore restore)
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

        // Восстанавливаем значение до урона, включая улучшения и активные способности.
        // Гравитацию урон не меняет: повторно применять базовые свойства класса нельзя.
        player.SetSpeed(restore.Speed);
    }

    private sealed record PendingRestore(nint PawnAddress, IZombie Zombie, float Speed);
}
