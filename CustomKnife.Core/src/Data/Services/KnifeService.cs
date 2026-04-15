using Common.Di;
using CustomKnife.Data.Models;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Utils.Extensions;
using CustomKnife.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomKnife.Data.Services;

public class KnifeService(ISwiftlyCore core) : IKnifeService
{
    private const string DefaultKnifeName = "weapon_knife";
    private const string CustomKnifeName = "weapon_knife_t";

    private const float DefaultSpeed = 250f;
    private const float DefaultGravity = 800f;

    public bool TryGiveKnife(IPlayer? player)
    {
        if (!CanHasKnife(player))
        {
            return false;
        }
        
        GiveKnife(player!);

        return true;
    }

    public void ChangeKnife(IPlayer player, IKnife knife)
    {
        CustomKnife.PlayerKnifes[player] = knife;
            
        TryGiveKnife(player);
    }

    public bool TryApplyProperties(IPlayer? player)
    {
        if (player == null || !player.IsValid || !player.IsAlive || CustomKnife.ZombiePlagueApi.IsInfected(player))
        {
            return false;
        }
        
        var weaponService = player.PlayerPawn?.WeaponServices;
        var activeWeapon = weaponService?.ActiveWeapon.Value;
        
        if (activeWeapon == null)
        {
            return false;
        }
        
        var isKnife = activeWeapon.DesignerName.Contains("knife");
        
        if (!isKnife)
        {
            ApplyDefaultProperties(player);
            
            return false;
        }

        ApplyKnifeProperties(player);

        return true;
    }

    public bool TryApplyKnifeKnockback(EventPlayerHurt @event)
    {
        var attacker = @event.AttackerPlayer;

        if (attacker == null || !attacker.IsValid)
        {
            return false;
        }

        if (!@event.Weapon.Contains("knife"))
        {
            return false;
        }
        
        var attackerKnife = GetKnife(attacker);
        
        CustomKnife.ZombiePlagueApi.ApplyKnockBack(@event, attackerKnife.KnockbackData);

        return true;
    }

    public IKnife GetKnife(IPlayer player)
    {
        if (CustomKnife.PlayerKnifes.TryGetValue(player, out var knife))
        {
            return knife;
        }

        return CustomKnife.PlayerKnifes[player] = CustomKnife.RegisteredKnifes[0];
    }

    public bool TryApplyKnifeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = @event.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker == null || !attacker.IsValid || !attacker.IsAlive || CustomKnife.ZombiePlagueApi.IsInfected(attacker))
        {
            return false;
        }
        
        var victim = @event.Entity.Address.FindPlayerByPawnAddress();

        if (victim == null || !victim.IsValid || !victim.IsAlive)
        {
            return false;
        }

        if (@event.Info.DamageType != DamageTypes_t.DMG_SLASH)
        {
            return false;
        }

        var weapon = attacker.PlayerPawn?.WeaponServices?.ActiveWeapon.Value;

        if (weapon == null || !weapon.DesignerName.Contains("knife"))
        {
            return false;
        }
        
        var knife = GetKnife(attacker);

        @event.Info.Damage *= knife.DamageMultiplier;

        return true;
    }

    public List<IKnife> GetRegisteredKnives()
    {
        List<IKnife> registeredKnifes = [];
        
        if (CustomKnife.RegisteredKnifes.Count != 0)
            return CustomKnife.RegisteredKnifes;
        
        var knives = DependencyResolver
            .GetRequiredService<CustomKnifeModule, IEnumerable<IKnife>>()
            .OrderBy(k => k.Index)
            .ToList();
        
        foreach (var knife in knives)
        {
            registeredKnifes.Add(knife);
        }
        
        return registeredKnifes;
    }

    private void ApplyKnifeProperties(IPlayer player)
    {
        var knife = GetKnife(player);
        player.SetSpeed(knife.Speed);
        player.SetGravity(knife.Gravity);
    }

    private void ApplyDefaultProperties(IPlayer player)
    {
        player.SetSpeed(DefaultSpeed);
        player.SetGravity(DefaultGravity);
    }

    private bool CanHasKnife(IPlayer? player)
    {
        if (player == null)
        {
            return false;
        }
        
        if (CustomKnife.ZombiePlagueApi.IsInfected(player))
        {
            return false;
        }

        var playerPawn = player.PlayerPawn;

        if (playerPawn == null || !player.IsValid)
        {
            return false;
        }

        if (!player.IsAlive)
        {
            return false;
        }

        return true;
    }

    private void RemoveOldAndGiveNewKnife(CCSPlayer_WeaponServices weaponService, CCSPlayer_ItemServices itemService)
    {
        weaponService.RemoveWeaponByDesignerName(DefaultKnifeName);
        itemService.GiveItem(CustomKnifeName);
    }

    private CBasePlayerWeapon ModifyKnife(IPlayer player, IKnife knife)
    {
        var weaponService = player.PlayerPawn?.WeaponServices;
        
        var playerKnife = weaponService?.MyValidWeapons.ToList().Find(w => w.DesignerName.Contains("knife"));
        var attributeManager = playerKnife?.AttributeManager;
        
        playerKnife?.AcceptInput("ChangeSubclass", "59");
        playerKnife?.SetModel(knife.Model);

        attributeManager?.Item.CustomName = knife.DisplayName;
        attributeManager?.Item.CustomNameUpdated();

        return playerKnife!;
    }

    private void SelectKnifeInNextTick(CCSPlayer_WeaponServices weaponService, CBasePlayerWeapon knife)
    {
        core.Scheduler.NextTick(() => weaponService.SelectWeapon(knife));
    }
    
    private void GiveKnife(IPlayer player)
    {
        core.Scheduler.NextWorldUpdate(() =>
        {
            var playerPawn = player.PlayerPawn!;
            var weaponService = playerPawn.WeaponServices;
            var itemService = playerPawn.ItemServices;

            if (weaponService == null || itemService == null)
            {
                return;
            }

            RemoveOldAndGiveNewKnife(weaponService, itemService);

            var knife = GetKnife(player);
            var newKnife = ModifyKnife(player, knife);

            SelectKnifeInNextTick(weaponService, newKnife);
        });
    }

}