using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace Common.Effects.Effects;

public sealed class Freeze(ISwiftlyCore core, IPlayer? caster, IPlayer target) : BaseEffect(core, caster, target)
{
    public override float Duration => 5.0f;
    public override void Destroy()
    {
        DestroyEffect();
    }

    private readonly Color _freezeRender = new(127, 127, 255);
    private readonly Color _defaultRender = new(255, 255, 255, 255);
    
    private const string FreezeSoundName = "FrostNade.hit";
    private const string UnFreezeSoundName = "FrostNade.end";

    protected override void ApplyEffect()
    {
        ApplyFreezeEffect();
        PlaySound(FreezeSoundName);
    }

    protected override void DestroyEffect()
    {
        ApplyUnfreezeEffect();
        PlaySound(UnFreezeSoundName);
        base.DestroyEffect();
    }

    private void ApplyFreezeEffect()
    {
        var targetPawn = Target.PlayerPawn;
        targetPawn?.MoveType = MoveType_t.MOVETYPE_FLY;
        targetPawn?.ActualMoveType = MoveType_t.MOVETYPE_FLY;
        targetPawn?.MoveTypeUpdated();
        
        targetPawn?.AbsVelocity = Vector.Zero;

        targetPawn?.Render = _freezeRender;
        targetPawn?.RenderUpdated();
    }

    private void ApplyUnfreezeEffect()
    {
        var targetPawn = Target.PlayerPawn;
        targetPawn?.MoveType = MoveType_t.MOVETYPE_WALK;
        targetPawn?.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        targetPawn?.MoveTypeUpdated();

        targetPawn?.Render = _defaultRender;
        targetPawn?.RenderUpdated();
    }
    
    public override void PlaySound(string soundName)
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 1.0f,
            Name = soundName,
            SourceEntityIndex = (int)Target.PlayerPawn!.Index
        };
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
    }
}