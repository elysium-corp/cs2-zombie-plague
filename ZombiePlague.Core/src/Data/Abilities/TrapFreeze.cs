using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class TrapFreeze(
    ISwiftlyCore core,
    PlayerPawnReference target,
    MoveType_t previousMoveType,
    MoveType_t previousActualMoveType,
    Action<TrapFreeze> onFinished) : IDisposable
{
    public IEffect? Disorientation { get; set; }
    private CancellationTokenSource? _timer;
    private bool _disposed;

    public void Schedule(float duration)
    {
        if (_disposed || _timer is not null) return;
        _timer = core.Scheduler.DelayBySeconds(Math.Max(0.1f, duration), Dispose);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var timer = _timer;
        var disorientation = Disorientation;
        _timer = null;
        Disorientation = null;
        try
        {
            try
            {
                timer?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Scheduler уже освободил таймер.
            }
            finally
            {
                // Если pawn заменён или движение уже изменено другой механикой,
                // параметры старой ловушки к нему не применяются.
                if (target.TryResolve(core, out var pawn) &&
                    pawn.MoveType == MoveType_t.MOVETYPE_NONE &&
                    pawn.ActualMoveType == MoveType_t.MOVETYPE_NONE)
                {
                    pawn.MoveType = previousMoveType;
                    pawn.ActualMoveType = previousActualMoveType;
                    pawn.MoveTypeUpdated();
                }
            }
        }
        finally
        {
            try
            {
                disorientation?.Destroy();
            }
            finally
            {
                onFinished(this);
            }
        }
    }
}
