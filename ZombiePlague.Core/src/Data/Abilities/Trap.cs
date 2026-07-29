using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Trap(ISwiftlyCore core, TrapConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CBaseModelEntity? _trapEntity;
    private CancellationTokenSource? _trapThinker;
    private CancellationTokenSource? _trapEffectToken;
    private IPlayer? _trappedPlayer;
    private MoveType_t? _trappedMoveType;
    private MoveType_t? _trappedActualMoveType;
    private int _spawnVersion;

    private const float Delay = 0.1f;

    public override void Use()
    {
        StopTrap();
        var spawnVersion = _spawnVersion;

        core.Scheduler.NextTick(() =>
        {
            var casterPawn = Caster.PlayerPawn;
            var casterPosition = casterPawn?.AbsOrigin;

            if (
                spawnVersion != _spawnVersion ||
                !Caster.IsValid ||
                !Caster.IsAlive ||
                casterPawn is not { IsValid: true } ||
                casterPosition is null
            )
            {
                return;
            }

            _trapEntity = core.EntitySystem.CreateEntity<CBaseModelEntity>();
            _trapEntity.SetModel(string.Empty);
            _trapEntity.Render = new Color(255, 255, 255, 0);
            _trapEntity.DispatchSpawn();
            _trapEntity.Teleport(casterPosition.Value, null, null);

            var filter = new CRecipientFilter(NetChannelBufType_t.BUF_RELIABLE);
            filter.AddRecipient(Caster.PlayerID);

            core.Engine.DispatchParticleEffect(
                config.ParticleEffectName,
                ParticleAttachment_t.PATTACH_ABSORIGIN,
                0,
                string.Empty,
                filter,
                resetAllParticlesOnEntity: false,
                splitScreenSlot: 0,
                _trapEntity
            );

            StartTrapThinker();
        });

        base.Use();
    }

    protected override bool CanUse()
    {
        return
            Caster is { IsValid: true, IsAlive: true } &&
            Caster.RequiredPlayerPawn.GroundEntity.Value != null;
    }

    public override void UnHook()
    {
        StopTrap();
        base.UnHook();
    }

    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        var soundName = config.SoundEffectNames[
            Random.Shared.Next(config.SoundEffectNames.Count)
        ];

        if (string.IsNullOrWhiteSpace(soundName))
        {
            return;
        }

        using var sound = new SoundEvent(soundName);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;
        sound.Emit();
    }

    private void StartTrapThinker()
    {
        var elapsedTime = 0f;

        _trapThinker = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            var trapEntity = _trapEntity;
            var trapPosition = trapEntity?.AbsOrigin;

            if (
                trapEntity is not { IsValidEntity: true } ||
                trapPosition is null ||
                !Caster.IsValid ||
                !Caster.IsAlive ||
                elapsedTime >= config.LiveDuration
            )
            {
                DespawnTrapEntity();
                return;
            }

            var foundPlayers = MathAlgorithm.FindAllPlayersInSphere(
                config.TriggerRadius,
                trapPosition.Value
            );

            foreach (var foundPlayer in foundPlayers)
            {
                if (
                    foundPlayer.IsValid &&
                    foundPlayer.IsAlive &&
                    foundPlayer.PlayerID != Caster.PlayerID &&
                    foundPlayer.Controller.Team != Caster.Controller.Team
                )
                {
                    TrapPlayer(foundPlayer);
                    return;
                }
            }

            elapsedTime += Delay;
        });
    }

    private void TrapPlayer(IPlayer player)
    {
        DespawnTrapEntity();

        if (
            !player.IsValid ||
            !player.IsAlive ||
            player.Controller.Team == Caster.Controller.Team ||
            player.PlayerPawn is not { IsValid: true } pawn
        )
        {
            return;
        }

        _trappedPlayer = player;
        _trappedMoveType = pawn.MoveType;
        _trappedActualMoveType = pawn.ActualMoveType;

        pawn.MoveType = MoveType_t.MOVETYPE_NONE;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_NONE;
        pawn.MoveTypeUpdated();
        pawn.AbsVelocity = Vector.Zero;

        var elapsedTime = 0f;

        _trapEffectToken = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            elapsedTime += Delay;

            if (
                !Caster.IsValid ||
                !Caster.IsAlive ||
                !player.IsValid ||
                !player.IsAlive ||
                player.Controller.Team == Caster.Controller.Team ||
                elapsedTime >= config.EffectDuration
            )
            {
                ReleaseTrappedPlayer();
            }
        });
    }

    private void StopTrap()
    {
        _spawnVersion++;
        DespawnTrapEntity();
        ReleaseTrappedPlayer();
    }

    private void DespawnTrapEntity()
    {
        _trapThinker?.Cancel();
        _trapThinker = null;

        if (_trapEntity is { IsValidEntity: true })
        {
            _trapEntity.Despawn();
        }

        _trapEntity = null;
    }

    private void ReleaseTrappedPlayer()
    {
        _trapEffectToken?.Cancel();
        _trapEffectToken = null;

        if (_trappedPlayer?.PlayerPawn is { IsValid: true } pawn)
        {
            pawn.MoveType = _trappedMoveType ?? MoveType_t.MOVETYPE_WALK;
            pawn.ActualMoveType = _trappedActualMoveType ?? MoveType_t.MOVETYPE_WALK;
            pawn.MoveTypeUpdated();
        }

        _trappedPlayer = null;
        _trappedMoveType = null;
        _trappedActualMoveType = null;
    }
}