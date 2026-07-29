using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal class Trap(ISwiftlyCore core, TrapConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;
    public override float Cooldown => config.CooldownTime;

    private CBaseModelEntity? _trapEntity;
    private CancellationTokenSource? _trapThinker;
    private IPlayer? _trappedPlayer;
    private CancellationTokenSource? _trapEffectToken;
    
    private const float Delay = 0.1f;

    public override void Use()
    {
        DespawnTrap();

        core.Scheduler.NextTick(() =>
        {
            var casterPos = Caster.RequiredPawn.AbsOrigin;
            _trapEntity = core.EntitySystem.CreateEntity<CBaseModelEntity>();
            _trapEntity.SetModel("");
            _trapEntity.Render = new Color(255, 255, 255, 0);
            _trapEntity.DispatchSpawn();
            
            _trapEntity.Teleport(casterPos, null, null);

            var filter = new CRecipientFilter(NetChannelBufType_t.BUF_RELIABLE);
            filter.AddRecipient(Caster.PlayerID);

            core.Engine.DispatchParticleEffect(config.ParticleEffectName,
                ParticleAttachment_t.PATTACH_ABSORIGIN,
                0,
                string.Empty,
                filter,
                resetAllParticlesOnEntity: false,
                splitScreenSlot: 0,
                _trapEntity);
            
            PlaySound();
            
            SetTrapThinker();
        });

        base.Use();
    }

    protected override bool CanUse()
    {
        if (!Caster.IsValid)
        {
            return false;
        }

        if (!Caster.IsAlive)
        {
            return false;
        }

        if (!Caster.IsOnZombieTeam())
        {
            return false;
        }

        if (Caster.RequiredPlayerPawn.GroundEntity.Value == null)
        {
            return false;
        }

        return true;
    }

    private void SetTrapThinker()
    {
        var startTime = 0f;
        
        _trapThinker = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            var trapEntity = _trapEntity;
            if (trapEntity == null || !trapEntity.IsValidEntity || !Caster.IsAlive ||
                startTime >= config.LiveDuration)
            {
                DespawnTrap();
                return;
            }
        
            var foundPlayers = MathAlgorithm.FindAllPlayersInSphere(
                core,
                config.TriggerRadius,
                trapEntity.AbsOrigin!.Value);
            if (foundPlayers.Count > 0)
            {
                foreach (var foundPlayer in foundPlayers)
                {
                    if (!foundPlayer.IsOnZombieTeam() && !foundPlayer.Equals(Caster))
                    {
                        TrapPlayer(foundPlayer);
                        break;
                    }
                }
            }
            startTime += Delay;
        });
    }

    private void TrapPlayer(IPlayer player)
    {
        DespawnTrap();
        CancelTrapEffect();

        player.PlayerPawn?.MoveType = MoveType_t.MOVETYPE_NONE;
        player.PlayerPawn?.ActualMoveType = MoveType_t.MOVETYPE_NONE;
        player.PlayerPawn?.MoveTypeUpdated();
        player.PlayerPawn?.AbsVelocity = new Vector(0, 0, 0);

        var startTime = 0f;
        _trappedPlayer = player;

        _trapEffectToken = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            startTime += Delay;

            if (!player.IsValid || player.IsOnZombieTeam() || !player.IsAlive)
            {
                CancelTrapEffect();
                return;
            }

            if (startTime >= config.EffectDuration)
            {
                CancelTrapEffect();
            }
        });
    }

    private void UnTrapPlayer(IPlayer player)
    {
        player.PlayerPawn?.MoveType = MoveType_t.MOVETYPE_WALK;
        player.PlayerPawn?.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        player.PlayerPawn?.MoveTypeUpdated();
    }

    private void DespawnTrap()
    {
        if (_trapEntity is { IsValidEntity: true })
        {
            _trapEntity.Despawn();
        }

        _trapThinker?.Cancel();
        _trapThinker = null;
        _trapEntity = null;
    }

    private void CancelTrapEffect()
    {
        _trapEffectToken?.Cancel();
        _trapEffectToken = null;

        if (_trappedPlayer != null)
        {
            UnTrapPlayer(_trappedPlayer);
            _trappedPlayer = null;
        }
    }

    public override void UnHook()
    {
        DespawnTrap();
        CancelTrapEffect();
        base.UnHook();
    }

    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        using var sound = new SoundEvent(config.SoundEffectNames[0]);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;

        sound.Emit();
    }
}
