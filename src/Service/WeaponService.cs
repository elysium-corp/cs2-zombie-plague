using CS2ZombiePlague.Data.Weapons;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Mappers;
using CS2ZombiePlague.Data.Weapons.Utils.Extensions;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service.Contracts;
using CS2ZombiePlague.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Schemas;

namespace CS2ZombiePlague.Service;

public sealed class WeaponService : IWeaponService
{
    private readonly List<BaseWeapon> _weapons = [];

    private readonly ISwiftlyCore _core;
    private readonly IWeaponFactory _weaponFactory;
    private readonly IGrenadeFactory _grenadeFactory;
    private readonly IWeaponRegistrator _weaponRegistrator = DependencyManager.GetService<IWeaponRegistrator>();

    private const string WeaponPrefix = "weapon_";
    
    public WeaponService(ISwiftlyCore core, IWeaponFactory weaponFactory, IGrenadeFactory grenadeFactory)
    {
        _core = core;
        _weaponFactory = weaponFactory;
        _grenadeFactory = grenadeFactory;
        
        _core.Event.OnEntityDeleted += OnEntityDeleted;
    }

    public void Dispose()
    {
        _core.Event.OnEntityDeleted -= OnEntityDeleted;
    }
    
    private void OnEntityDeleted(IOnEntityDeletedEvent @event)
    {
        var entity = @event.Entity;
        var weapon = _weapons.Find(wp => wp.AttachedWeapon?.Index == entity.Index);

        if (weapon != null)
        {
            _weapons.Remove(weapon);
        }
    }

    /// <summary>
    /// Получает активные (существующие на данный момент) кастомные пушки на сервере.
    /// </summary>
    /// <remarks>
    /// Это не список всех оружий на сервере, а только те, которые были созданы в течение раунда или уже экипированы.
    /// </remarks>
    public List<BaseWeapon> GetAllWeapons()
    {
        return _weapons;
    }

    public BaseWeapon? GetWeaponByIndex(int index)
    {
        return _weapons.Find(wp => wp.AttachedWeapon.Index == index);
    }
    
    public BaseWeapon? GetWeaponByIndex(uint index)
    {
        return _weapons.Find(wp => wp.AttachedWeapon.Index == index);
    }

    /// <summary>
    /// Выдает оружие типа <b>IWeapon</b>.
    /// </summary>
    /// <returns>
    /// <b>IWeapon</b>, если удалось создать пушку и выдать игроку <b>player</b>; иначе <b>null</b>.
    /// </returns>
    public BaseWeapon? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon
    {
        return GiveWeaponInternal(
            player: player,
            createWeapon: () => _weaponFactory.Create<TWeapon>(),
            giveAndResolveWeapon: (itemService, weaponService, customWeapon) =>
            {
                var inheritorName = ResolveInheritorName(customWeapon.InheritorName);
                itemService.GiveItem(inheritorName);

                return weaponService.MyValidWeapons
                    .FirstOrDefault(w => w.DesignerName == customWeapon.InheritorName);
            }
        );
    }

    /// <summary>
    /// Выдает оружие типа <b>IWeapon</b>.
    /// <b>TWeapon</b> - тип кастомной пушки, <b>TSchemaWeapon</b> - базовое оружие от которого наследуется кастомная пушка.
    /// </summary>
    /// <returns>
    /// <b>IWeapon</b>, если удалось создать пушку и выдать игроку <b>player</b>; иначе <b>null</b>.
    /// </returns>
    public BaseWeapon? GiveWeapon<TWeapon, TSchemaWeapon>(IPlayer player)
        where TWeapon : BaseWeapon
        where TSchemaWeapon : class, ISchemaClass<TSchemaWeapon>, CBasePlayerWeapon
    {
        return GiveWeaponInternal(
            player: player,
            createWeapon: () => _weaponFactory.Create<TWeapon>(),
            giveAndResolveWeapon: (itemService, _, _) => itemService.GiveItem<TSchemaWeapon>()
        );
    }

    public BaseWeapon? GiveWeapon(IPlayer player, string internalName)
    {
        var foundWeapon = _weaponRegistrator
            .GetAllWeapons()
            .Find(w => w.InternalName.Contains(internalName));

        if (foundWeapon == null)
        {
            return null;
        }

        return GiveWeaponInternal(
            player: player,
            createWeapon: () => _weaponFactory.Create(foundWeapon.InternalName),
            giveAndResolveWeapon: (itemService, weaponService, customWeapon) =>
            {
                var slot = customWeapon.Slot.MapToGearSlot();
                weaponService.DropWeaponBySlot(slot);
                
                var inheritorName = ResolveInheritorName(customWeapon.InheritorName);
                itemService.GiveItem(inheritorName);
                
                return weaponService.MyValidWeapons
                    .FirstOrDefault(w => w.DesignerName.Contains(customWeapon.InheritorName));
            }
        );
    }

    public BaseGrenade? GiveGrenade(IPlayer player, string internalName)
    {
        var foundGrenade = _weaponRegistrator
            .GetAllWeapons()
            .Find(w => w.InternalName.Contains(internalName))
            .As<BaseGrenade>();

        if (foundGrenade == null)
        {
            return null;
        }

        return GiveGrenadeInternal(
            player: player,
            createGrenade: () => _grenadeFactory.Create(foundGrenade.InternalName),
            giveAndResolveGrenade: (itemService, weaponService, customGrenade) =>
            {
                var inheritorName = ResolveInheritorName(customGrenade.InheritorName);
                weaponService.DropWeaponByDesignerName(inheritorName);
                itemService.GiveItem(inheritorName);
                
                return weaponService.MyValidWeapons
                    .FirstOrDefault(w => w.DesignerName.Contains(customGrenade.InheritorName));
            }
        );
    }

    private BaseGrenade? GiveGrenadeInternal(
        IPlayer player,
        Func<BaseGrenade> createGrenade,
        Func<CCSPlayer_ItemServices, CCSPlayer_WeaponServices, IWeapon, CBasePlayerWeapon?> giveAndResolveGrenade)
    {
        var pawn = player.RequiredPlayerPawn;

        var itemService = pawn.ItemServices;
        if (itemService == null)
        {
            return null;
        }

        var weaponService = pawn.WeaponServices;
        if (weaponService == null)
        {
            return null;
        }
        
        var customGrenade = createGrenade();
        var grenade = giveAndResolveGrenade(itemService, weaponService, customGrenade)?.As<CCSWeaponBase>();
        
        if (grenade == null)
        {
            return null;
        } 
        
        if (customGrenade.Model.IsNotNullOrEmpty())
        {
            grenade.SetModel(customGrenade.Model);
        }
        
        ModifyWeapon(customGrenade, ref grenade);
        
        return AddOrReplace(customGrenade).As<BaseGrenade>();
    }

    private BaseWeapon? GiveWeaponInternal(
        IPlayer player,
        Func<BaseWeapon> createWeapon,
        Func<CCSPlayer_ItemServices, CCSPlayer_WeaponServices, IWeapon, CBasePlayerWeapon?> giveAndResolveWeapon)
    {
        var pawn = player.RequiredPlayerPawn;

        var itemService = pawn.ItemServices;
        if (itemService == null)
        {
            return null;
        }

        var weaponService = pawn.WeaponServices;
        if (weaponService == null)
        {
            return null;
        }

        var customWeapon = createWeapon();
        var weapon = giveAndResolveWeapon(itemService, weaponService, customWeapon)?.As<CCSWeaponBase>();
        
        if (weapon == null)
        {
            return null;
        } 
        
        if (customWeapon.Model.IsNotNullOrEmpty())
        {
            weapon.SetModel(customWeapon.Model);
        }
        
        ModifyWeapon(customWeapon, ref weapon); 
        
        return AddOrReplace(customWeapon);
    }

    private void ModifyWeapon(BaseWeapon source, ref CCSWeaponBase weapon)
    {
        if (source.Model.IsNotNullOrEmpty())
        {
            weapon.SetModel(source.Model);
        }

        weapon.AttributeManager.Item.CustomName = source.DisplayName;
        weapon.AttributeManager.Item.CustomNameOverride = source.DisplayName;
        weapon.AttributeManager.Item.CustomNameUpdated();
        
        source.AttachedWeapon = weapon;
        
        _core.PlayerManager.SendChat($"source = {source.AttachedWeapon.Index}, weapon = {weapon.Index}");
    }

    private BaseWeapon AddOrReplace(BaseWeapon weapon)
    {
        var foundWeapon = _weapons.Find(wp => wp.AttachedWeapon.Index == weapon.AttachedWeapon.Index);

        if (foundWeapon == null)
        {
            _weapons.Add(weapon);
            return weapon;
        }

        var index = _weapons.IndexOf(foundWeapon);
        _weapons[index] = foundWeapon;
        return foundWeapon;
    }

    private string ResolveInheritorName(string inheritorName)
    {
        if (inheritorName.Contains(WeaponPrefix))
        {
            return inheritorName;
        }

        return $"{WeaponPrefix}{inheritorName}";
    }
}