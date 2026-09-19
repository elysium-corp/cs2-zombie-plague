using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal sealed class DamageMovementRestore(ISwiftlyCore core, IPlayerManager playerManager)
{
    private const int PlayerDamageRestoreDelay = 20;

    private readonly Dictionary<ulong, PendingRestore> _pending = [];

    public void Schedule(IPlayer? player, bool afterPlayerDamage = false)
    {
        if (player is not { IsValid: true, IsAlive: true } ||
            player.PlayerPawn is not { IsValid: true } pawn ||
            !playerManager.TryGetZombie(player, out var zombie))
        {
            return;
        }

        var sessionId = player.SessionId;
        if (!afterPlayerDamage &&
            _pending.TryGetValue(sessionId, out var pending) &&
            pending.PawnAddress == pawn.Address && ReferenceEquals(pending.Zombie, zombie))
        {
            return;
        }

        var restore = new PendingRestore(pawn.Address, zombie);
        _pending[sessionId] = restore;

        if (afterPlayerDamage)
        {
            // Урон от игрока завершает применение штрафов движения уже после TakeDamage.Pre.
            // Новое попадание заменяет ожидающее восстановление, чтобы применить его после последнего удара.
            core.Scheduler.Delay(PlayerDamageRestoreDelay, () => Restore(sessionId, restore));
            return;
        }

        // Несколько непользовательских попаданий за один тик требуют только одного восстановления.
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

        // Параметры берём из класса зомби на момент восстановления.
        player.SetSpeed(zombie.ZClass.Speed);
        player.SetGravity(zombie.ZClass.Gravity);
    }

    private sealed record PendingRestore(nint PawnAddress, IZombie Zombie);
}
