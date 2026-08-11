using Common.Effects.Effects.Contracts;
using Common.Effects.Effects.Settings;
using Common.Effects.Effects.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects;

public sealed class Freeze(
    ISwiftlyCore core,
    Action<IEffect> callback,
    IPlayer? caster,
    IPlayer target,
    FreezeSettings? settings) : BaseEffect(core, callback, caster, target)
{
    public FreezeSettings Settings { get; } = settings ?? new FreezeSettings();

    public override float Duration => Settings.Duration;

    public override void Destroy()
    {
        DestroyEffect();
    }

    private readonly Color _freezeRender = new(127, 191, 255);
    private readonly Color _defaultRender = new(255, 255, 255, 255);

    private const string FreezeSoundName = "FrostNade.hit";
    private const string UnfreezeSoundName = "FrostNade.end";

    protected override void ApplyEffect()
    {
        PlaySound(FreezeSoundName);

        var targetPawn = Target.PlayerPawn;
        targetPawn?.MoveType = MoveType_t.MOVETYPE_FLY;
        targetPawn?.ActualMoveType = MoveType_t.MOVETYPE_FLY;
        targetPawn?.MoveTypeUpdated();

        targetPawn?.AbsVelocity = Vector.Zero;

        CreateParticle();

        Core.GameHooks.Entities.TakeDamage.Pre += OnTakeDamage;
    }

    protected override void DestroyEffect()
    {
        RemoveEffect();
        PlaySound(UnfreezeSoundName);
        base.DestroyEffect();
    }

    protected override void PlaySound(string soundName)
    {
        SoundExt.PlayAt(Target, soundName, 1f);
    }

    private void RemoveEffect()
    {
        var targetPawn = Target.PlayerPawn;
        targetPawn?.MoveType = MoveType_t.MOVETYPE_WALK;
        targetPawn?.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        targetPawn?.MoveTypeUpdated();

        DestroyParticle();

        Core.GameHooks.Entities.TakeDamage.Pre -= OnTakeDamage;
    }

    protected override void CreateParticle()
    {
        var targetPawn = Target.PlayerPawn;

        targetPawn?.Render = _freezeRender;
        targetPawn?.RenderUpdated();

        core.NetMessage.SendCUserMessageFade(
            playerId: Target.PlayerID,
            duration: (uint)Settings.Duration * 1000,
            holdTime: 100,
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(_freezeRender.R, _freezeRender.G, _freezeRender.B, 128)
        );
    }

    protected override void DestroyParticle()
    {
        var targetPawn = Target.PlayerPawn;

        targetPawn?.Render = _defaultRender;
        targetPawn?.RenderUpdated();

        core.NetMessage.SendCUserMessageFade(playerId: Target.PlayerID,
            duration: 0,
            holdTime: 0,
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(0, 0, 0, 0));
    }

    private void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim == null || victim.PlayerPawn == null || !victim.IsValid || !victim.IsAlive) return;

        if (victim.PlayerID != Target.PlayerID) return;

        context.Params.Info.Damage *= Settings.DamageReduction;
    }
}