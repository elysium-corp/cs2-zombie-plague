using Common.Effects.Effects.Contracts;
using Common.Effects.Effects.Settings;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Common.Effects.Effects;

public sealed class Disorient(
    ISwiftlyCore core,
    Action<IEffect> callback,
    IPlayer? caster,
    IPlayer target,
    DisorientSettings? settings)
    : BaseTickEffect(core, callback, caster, target)
{
    public DisorientSettings Settings { get; } = settings ?? new DisorientSettings();

    public override float Duration => Settings.Duration;
    protected override float TickInterval => 0.05f;
    private const float Amplitude = 25f;
    private const float Frequency = 18f;
    private const uint Command = 0;

    public override void Destroy()
    {
        base.DestroyEffect();
    }

    protected override bool CanApply()
    {
        if (!Target.IsValid || !Target.IsAlive) return false;

        return true;
    }

    protected override void ApplyEffect()
    {
        ApplyShakeEffect();

        ApplyScreenEffect();
    }

    protected override void TickEffect()
    {
        var playerPawn = Target.PlayerPawn;

        if (playerPawn == null || !playerPawn.IsValidEntity) return;

        var pitch = (Random.Shared.NextSingle() - 0.5f) * 2.5f;
        var yaw = (Random.Shared.NextSingle() - 0.5f) * 4.0f;

        var angles = playerPawn.EyeAngles;

        angles.X += pitch;
        angles.Y += yaw;

        angles.X = Math.Clamp(angles.X, -89f, 89f);

        playerPawn.EyeAngles = angles;
    }

    private void ApplyScreenEffect()
    {
        Core.NetMessage.Send<CUserMessageFade>(msg =>
        {
            msg.Duration = 1000;
            msg.HoldTime = (uint)Duration * 1000 / 2;
            msg.Flags = 0x0001 | 0x0004;
            msg.Color = Rgba(0, 80, 0, 50);
            msg.SendToPlayer(Target.PlayerID);
        });
    }

    private void ApplyShakeEffect()
    {
        Core.NetMessage.Send<CUserMessageShake>(msg =>
        {
            msg.Command = Command;
            msg.Amplitude = Amplitude;
            msg.Duration = Duration;
            msg.Frequency = Frequency;
            msg.SendToPlayer(Target.PlayerID);
        });
    }

    private uint Rgba(byte r, byte g, byte b, byte a)
        => (uint)(r | (g << 8) | (b << 16) | (a << 24));
}