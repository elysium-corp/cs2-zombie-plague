using System.Diagnostics.CodeAnalysis;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Utils;
using CS2ZombiePlague.Data.Weapons.Utils.Extensions;
using CS2ZombiePlague.Service;
using CS2ZombiePlague.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Controller;

public sealed class GrenadeHandler(
    IPlayer owner, 
    ISwiftlyCore core, 
    WeaponService weaponService
    )
{
    private readonly Dictionary<string, BaseGrenade> _usedGrenades = new();

    public HookResult OnGrenadeThrownPre(EventGrenadeThrown gameEvent)
    {
        var player = core.PlayerManager.GetPlayer(gameEvent.UserId);

        if (!IsOwner(player))
        {
            return HookResult.Continue;
        }

        var grenadeName = gameEvent.Weapon;
        var playerWeaponService = player.RequiredPlayerPawn.WeaponServices;
        var originalGrenade = playerWeaponService?.FindWeaponByName(grenadeName);

        if (originalGrenade == null)
        {
            return HookResult.Handled;
        }

        var grenade = weaponService.GetWeaponByIndex(originalGrenade.Index).As<BaseGrenade>();

        if (grenade == null)
        {
            return HookResult.Handled;
        }

        TryAdd(grenadeName, grenade);

        return HookResult.Handled;
    }

    public HookResult OnDecoyStartedPre(EventDecoyStarted gameEvent)
    {
        var player = core.PlayerManager.GetPlayer(gameEvent.UserId);

        if (!IsOwner(player))
        {
            return HookResult.Continue;
        }

        var grenadeId = gameEvent.EntityID;
        var grenade = TryGetGrenadeByIdOrNull(grenadeId);

        if (grenade == null)
        {
            return HookResult.Handled;
        }

        var pos = new Vector(gameEvent.X, gameEvent.Y, gameEvent.Z);

        grenade.OnDecoyStarted(pos);
        _usedGrenades.Remove(grenade.InheritorName);

        return HookResult.Handled;
    }

    public HookResult OnHegrenadeDetonatePre(EventHegrenadeDetonate gameEvent)
    {
        var player = core.PlayerManager.GetPlayer(gameEvent.UserId);

        if (!IsOwner(player))
        {
            return HookResult.Continue;
        }

        var grenadeId = gameEvent.EntityID;
        var grenade = TryGetGrenadeByIdOrNull(grenadeId);
        var grenadeEntity = core.EntitySystem.GetEntityByIndex<CBaseCSGrenadeProjectile>((uint)grenadeId);
        
        if (grenade == null)
        {
            return HookResult.Handled;
        }

        var pos = new Vector(gameEvent.X, gameEvent.Y, gameEvent.Z);

        grenade.OnHegrenadeDetonate(pos);
        _usedGrenades.Remove(grenade.InheritorName);
        
        grenadeEntity?.Damage *= grenade.DamageMultiplier;
        grenadeEntity?.DamageUpdated();
        grenadeEntity?.Despawn();
        
        return HookResult.Handled;
    }

    public HookResult OnMolotovDetonatePre(EventMolotovDetonate gameEvent)
    {
        var playerId = gameEvent.UserId;
        var player = core.PlayerManager.GetPlayer(playerId);

        if (!IsOwner(player)) return HookResult.Continue;
        
        var playerWeaponService = player.RequiredPlayerPawn.WeaponServices;
        var originalGrenade = playerWeaponService?.FindWeaponByName(WeaponName.Grenade.Inc)
                              ?? playerWeaponService?.FindWeaponByName(WeaponName.Grenade.Molotov);
        
        if (originalGrenade == null) return HookResult.Continue;
        
        var grenade = weaponService.GetWeaponByIndex(originalGrenade.Index).As<BaseGrenade>();

        if (grenade == null) return HookResult.Continue;

        var position = new Vector(gameEvent.X, gameEvent.Y, gameEvent.Z);
        
        grenade.OnMolotovDetonate(player, position);
        _usedGrenades.Remove(grenade.InheritorName);
        
        return HookResult.Continue;
    }
    
    public HookResult OnSmokegrenadeDetonatePre(EventSmokegrenadeDetonate gameEvent)
    {
        var player = core.PlayerManager.GetPlayer(gameEvent.UserId);

        if (!IsOwner(player))
        {
            return HookResult.Continue;
        }

        var grenadeId = gameEvent.EntityID;
        var grenade = TryGetGrenadeByIdOrNull(grenadeId);
        var grenadeEntity = core.EntitySystem.GetEntityByIndex<CBaseCSGrenadeProjectile>((uint)grenadeId);
        
        if (grenade == null)
        {
            return HookResult.Handled;
        }

        var pos = new Vector(gameEvent.X, gameEvent.Y, gameEvent.Z);

        grenade.OnSmokegrenadeDetonate(pos);
        _usedGrenades.Remove(grenade.InheritorName);
        
        grenadeEntity?.Despawn();
        
        return HookResult.Handled;
    }

    private BaseGrenade? TryGetGrenadeByIdOrNull(short grenadeId)
    {
        return TryGetGrenadeByIdOrNullInternal(grenadeId);
    }

    private BaseGrenade? TryGetGrenadeByIdOrNullInternal(int grenadeId)
    {
        var entityIndex = (uint)grenadeId;

        var projectileEntity = core.EntitySystem.GetEntityByIndex<CBaseCSGrenadeProjectile>(entityIndex);
        
        if (projectileEntity == null) return null;

        var primitiveGrenadeName = GrenadeMather.IfMatchedThenPrimitive(projectileEntity.DesignerName);
        
        if (primitiveGrenadeName == null) return null;

        _usedGrenades.TryGetValue(primitiveGrenadeName, out var grenadeByProjectile);
        
        return grenadeByProjectile;
    }

    private bool TryAdd(string name, BaseGrenade grenade)
    {
        var primitiveGrenadeName = GrenadeMather.IfMatchedThenPrimitive(name);

        if (primitiveGrenadeName == null) return false;

        return _usedGrenades.TryAdd(primitiveGrenadeName, grenade);
    }

    private bool IsOwner([NotNullWhen(true)] IPlayer? player)
    {
        return player?.PlayerID == owner.PlayerID;
    }
}