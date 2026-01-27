using CS2ZombiePlague.Config.Ability;
using CS2ZombiePlague.Data.Abilities.Contracts;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Abilities;

public class Trap(ISwiftlyCore core, TrapConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;
    public override float Cooldown => config.CooldownTime;

    private CBaseModelEntity? _trapEntity;
    private CancellationTokenSource? _trapThinker;

    private readonly CommonUtils _utils = DependencyManager.GetService<CommonUtils>();

    private const float Delay = 0.1f;

    public override void Use()
    {
        DespawnTrap();

        core.Scheduler.NextTick(() =>
        {
            var casterPos = Caster.RequiredPawn.AbsOrigin;
            _trapEntity = core.EntitySystem.CreateEntity<CBaseModelEntity>();
            _trapEntity.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            _trapEntity.Render = new Color(255, 255, 255, 0);

            _trapEntity.Teleport(casterPos, null, null);
            _trapEntity.DispatchSpawn();

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

        if (!Caster.IsInfected())
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
            if (!_trapEntity.IsValidEntity || !Caster.IsAlive || startTime >= config.LiveDuration)
            {
                DespawnTrap();
                return;
            }
        
            var foundPlayers = _utils.FindAllPlayersInSphere(config.TriggerRadius, _trapEntity.AbsOrigin.Value);
            if (foundPlayers.Count > 0)
            {
                foreach (var foundPlayer in foundPlayers)
                {
                    if (!foundPlayer.IsInfected() && !foundPlayer.Equals(Caster))
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

        player.PlayerPawn?.MoveType = MoveType_t.MOVETYPE_NONE;
        player.PlayerPawn?.ActualMoveType = MoveType_t.MOVETYPE_NONE;
        player.PlayerPawn?.MoveTypeUpdated();
        player.PlayerPawn?.AbsVelocity = new Vector(0, 0, 0);

        var startTime = 0f;

        CancellationTokenSource? token = null!;
        token = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            startTime += Delay;

            if (!player.IsValid || player.IsInfected() || !player.IsAlive)
            {
                token?.Cancel();
            }

            if (startTime >= config.EffectDuration || player.IsInfected() || !player.IsAlive)
            {
                UnTrapPlayer(player);
                token?.Cancel();
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
            _trapEntity?.Despawn(); 
        }
        _trapThinker?.Cancel();
    }

    public override void PlaySound()
    {
        using var sound = new SoundEvent(config.SoundEffectNames[0]);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;

        sound.Emit();
    }
}