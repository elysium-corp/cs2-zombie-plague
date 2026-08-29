using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectionBase(
    ISwiftlyCore core, 
    IPlayerManager playerManager,
    IOptions<ZombiePlagueCoreConfig> coreConfig
) : RoundBase(core, playerManager)
{
    protected override void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker is not { IsValid: true }) return;

        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim is not { IsValid: true } || !victim.IsAlive || victim.PlayerPawn is not { } pawn) return;

        if (!IsZombieAttackingHuman(attacker, victim)) return;

        var activeWeapon = attacker.PlayerPawn?
            .WeaponServices?
            .ActiveWeapon
            .Value;

        if (!InfectionDamagePolicy.IsKnifeAttack(context.Params.Info.DamageType, activeWeapon?.DesignerName))
        {
            SuppressDamage(ref context);
            return;
        }

        var armor = pawn.ArmorValue;

        if (armor > 0)
        {
            var armorDamage = InfectionDamagePolicy.GetArmorDamage(
                activeWeapon?.As<CCSWeaponBase>().WeaponMode ?? CSWeaponMode.Primary_Mode,
                coreConfig.Value
            );
            var remainingArmor = InfectionDamagePolicy.CalculateRemainingArmor(armor, armorDamage);

            victim.SetArmor(remainingArmor);

            // Броня управляется механикой Zombie Plague, а урон по здоровью
            // остаётся обычным и не должен повторно снимать броню движком.
            context.Params.Info.DamageFlags |= TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;

            return;
        }

        if (CanInfect(victim))
        {
            SuppressDamage(ref context);
            PlayerManager.TryInfect(victim, attacker);
        }
    }
    
    protected override HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        Core.Scheduler.NextWorldUpdate(() => SpawnAsZombie(player));

        return HookResult.Continue;
    }
    
    protected override HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        if (@event.Disconnect || @event.OldTeam != (byte)Team.Spectator || @event.Team != (byte)Team.T)
        {
            return HookResult.Continue;
        }

        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        Core.Scheduler.NextWorldUpdate(() => SpawnAsZombie(player));

        return HookResult.Continue;
    }
    
    public override bool TryRespawnPlayer(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive)
        {
            return false;
        }

        if (!PlayerManager.IsZombie(player) &&
            !PlayerManager.TryInfect(player))
        {
            return false;
        }

        return PlayerManager.TryRespawn(player);
    }

    private bool IsZombieAttackingHuman(IPlayer attacker, IPlayer victim)
    {
        return PlayerManager.IsZombie(attacker) && PlayerManager.IsHuman(victim);
    }

    private bool CanInfect(IPlayer victim)
    {
        if (!PlayerManager.IsHuman(victim)) return false;

        var aliveHumanCount = PlayerManager
            .GetAllHumans()
            .Count(player => player.IsAlive);

        return aliveHumanCount > 1;
    }

    private static void SuppressDamage(ref TakeDamageEntityPreContext context)
    {
        context.Params.Info.Damage = 0;
        context.SetHookResult(HookResult.CancelOriginal);
    }
    
    private void SpawnAsZombie(IPlayer player)
    {
        if (!player.IsValid)
        {
            return;
        }

        if (!PlayerManager.IsZombie(player) && !PlayerManager.TryInfect(player))
        {
            return;
        }

        if (!player.IsAlive)
        {
            PlayerManager.TryRespawn(player);
        }
    }
}
