using Common.Effects.Effects.Contracts;
using Common.Effects.Effects.Settings;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects;

public sealed class Vanish(
    ISwiftlyCore core,
    Action<IEffect> callback,
    IPlayer? caster,
    IPlayer target,
    VanishSettings? settings) : BaseEffect(core, callback, caster, target)
{
    public VanishSettings Settings { get; } = settings ?? new VanishSettings();
    public override float Duration => Settings.Duration;

    private readonly Color _invisibleRender = new(255, 255, 255, 0);
    private readonly Color _defaultRender = new(255, 255, 255, 255);

    protected override void ApplyEffect()
    {
        var targetPawn = Target.PlayerPawn;
        targetPawn?.Render = _invisibleRender;
        targetPawn?.RenderUpdated();
    }

    public override void Destroy()
    {
        DestroyEffect();
    }

    protected override void DestroyEffect()
    {
        var targetPawn = Target.PlayerPawn;
        targetPawn?.Render = _defaultRender;
        targetPawn?.RenderUpdated();
        base.DestroyEffect();
    }
}