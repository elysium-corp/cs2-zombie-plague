using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api;

namespace CustomKnife.Data.Services;

internal sealed class KnifeService(
    ISwiftlyCore core, 
    IKnivesRegistry knivesRegistry,
    IPlayerKnifeService playerKnifeService,
    IZombiePlagueApi zombiePlagueApi
) : IKnifeService
{
    private const string DefaultKnifeName = "weapon_knife";
    private const string CustomKnifeName = "weapon_knife_t";

    private const float DefaultSpeed = 250f;
    private const float DefaultGravity = 800f;
    

    public bool TryGiveKnife(IPlayer player)
    {
        if (!CanHaveKnife(player))
        {
            return false;
        }

        GiveKnife(player);

        return true;
    }

    public void SelectKnife(IPlayer player, IKnife knife)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(knife);

        if (!knivesRegistry.TryGet(knife.InternalName, out _))
        {
            throw new ArgumentException($"Knife '{knife.InternalName}' is not registered!", nameof(knife));
        }

        playerKnifeService.SetKnifeId(
            player.SteamID,
            knife.InternalName
        );

        TryGiveKnife(player);
    }

    public bool TryApplyProperties(IPlayer? player)
    {
        if (player == null || !player.IsValid || !player.IsAlive || zombiePlagueApi.IsInfected(player))
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

        zombiePlagueApi.ApplyKnockBack(@event, attackerKnife.KnockbackData);

        return true;
    }

    public IKnife GetKnife(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var knifeId = playerKnifeService.GetKnifeId(player.SteamID);

        if (knifeId is not null && knivesRegistry.TryGet(knifeId, out var knife))
        {
            return knife;
        }

        if (!knivesRegistry.TryGet(KnifeDefaults.DefaultKnifeId, out var defaultKnife))
        {
            throw new InvalidOperationException($"Default knife '{KnifeDefaults.DefaultKnifeId}' is not registered!");
        }

        return defaultKnife;
    }

    public bool TryApplyKnifeDamage(ref TakeDamageEntityPreContext @event)
    {
        var attacker = @event.Params.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker == null || !attacker.IsValid || !attacker.IsAlive ||
            zombiePlagueApi.IsInfected(attacker))
        {
            return false;
        }

        var victim = @event.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim == null || victim.PlayerPawn ==  null || !victim.PlayerPawn.IsValid || !victim.IsAlive)
        {
            return false;
        }

        if (@event.Params.Info.DamageType != DamageTypes_t.DMG_SLASH)
        {
            return false;
        }

        var weapon = attacker.PlayerPawn?.WeaponServices?.ActiveWeapon.Value;

        if (weapon == null || !weapon.DesignerName.Contains("knife"))
        {
            return false;
        }

        var knife = GetKnife(attacker);

        @event.Params.Info.Damage *= knife.DamageMultiplier;

        return true;
    }

    public IReadOnlyCollection<IKnife> GetRegisteredKnives()
    {
        return knivesRegistry.GetAll();
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

    private bool CanHaveKnife(IPlayer player)
    {
        return player.IsValid && player.IsAlive && player.PlayerPawn is { IsValid: true } && !zombiePlagueApi.IsInfected(player);
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

    private void SelectKnifeOnNextWorldUpdate(IPlayer player, CBasePlayerWeapon? knife)
    {
        if (!player.IsValid || knife is null || !knife.IsValid)
        {
            return;
        }

        var sessionId = player.SessionId;

        core.Scheduler.NextWorldUpdate(() =>
        {
            var currentPlayer = core.PlayerManager.GetPlayerFromSessionId(sessionId);

            if (currentPlayer is null || !currentPlayer.IsValid || !currentPlayer.IsAlive || !knife.IsValid)
            {
                return;
            }

            var weaponServices = currentPlayer.PlayerPawn?.WeaponServices;
            if (weaponServices is null)
            {
                return;
            }

            // Повторно проверяем, что нож существует
            // и действительно находится у этого игрока.
            var currentKnife = weaponServices
                .MyValidWeapons
                .FirstOrDefault(weapon => weapon.Address == knife.Address);

            if (currentKnife is null)
            {
                return;
            }

            weaponServices.SelectWeapon(currentKnife);
        });
    }

    private void GiveKnife(IPlayer player)
    {
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!player.IsValid)
            {
                return;
            }

            var playerPawn = player.PlayerPawn;
            if (playerPawn == null || !playerPawn.IsValid)
            {
                return;
            }

            var weaponService = playerPawn.WeaponServices;
            var itemService = playerPawn.ItemServices;

            if (weaponService == null || itemService == null)
            {
                return;
            }

            RemoveOldAndGiveNewKnife(weaponService, itemService);

            var knife = GetKnife(player);
            var newKnife = ModifyKnife(player, knife);

            SelectKnifeOnNextWorldUpdate(player, newKnife);
        });
    }
}