using CS2ZombiePlague.Data.Weapons;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Schemas;

namespace CS2ZombiePlague.Service;

public sealed class WeaponService : IWeaponService
{
    private readonly List<BaseWeapon> _weapons = [];

    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly IWeaponFactory _weaponFactory = DependencyManager.GetService<IWeaponFactory>();

    public WeaponService()
    {
        _core.Event.OnEntityDeleted += OnEntityDeleted;
    }

    public void Dispose()
    {
        _core.Event.OnEntityDeleted -= OnEntityDeleted;
    }
    
    private void OnEntityDeleted(IOnEntityDeletedEvent @event)
    {
        var entity = @event.Entity;
        var weapon = _weapons.Find(wp => wp.InheritorWeapon?.Index == entity.Index);

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
        return _weapons.Find(wp => wp.InheritorWeapon?.Index == index);
    }
    
    public BaseWeapon? GetWeaponByIndex(uint index)
    {
        return _weapons.Find(wp => wp.InheritorWeapon?.Index == index);
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
                itemService.GiveItem(customWeapon.InheritorName);

                return weaponService.MyValidWeapons
                    .FirstOrDefault(w =>
                    {
                        _core.PlayerManager.SendChat($"wp.DesignerName = {w.DesignerName}, customWeapon.InheritorName = {customWeapon.InheritorName}");
                        return w.DesignerName == customWeapon.InheritorName;
                    });
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
    public IWeapon? GiveWeapon<TWeapon, TSchemaWeapon>(IPlayer player)
        where TWeapon : BaseWeapon
        where TSchemaWeapon : class, ISchemaClass<TSchemaWeapon>, CBasePlayerWeapon
    {
        return GiveWeaponInternal(
            player: player,
            createWeapon: () => _weaponFactory.Create<TWeapon>(),
            giveAndResolveWeapon: (itemService, _, _) => itemService.GiveItem<TSchemaWeapon>()
        );
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

        weaponService.DropWeaponBySlot(customWeapon.Slot);

        var weapon = giveAndResolveWeapon(itemService, weaponService, customWeapon)?.As<CCSWeaponBase>();
        if (weapon == null)
        {
            return null;
        } 
        
        ModifyWeapon(ref customWeapon, ref weapon);

        return AddOrReplace(customWeapon);
    }

    private void ModifyWeapon(ref BaseWeapon source, ref CCSWeaponBase weapon)
    {
        weapon.SetModel(source.Model);

        weapon.AttributeManager.Item.CustomName = source.DisplayName;
        weapon.AttributeManager.Item.CustomNameOverride = source.DisplayName;
        weapon.AttributeManager.Item.CustomNameUpdated();
        
        source.InheritorWeapon = weapon;
    }

    private BaseWeapon AddOrReplace(BaseWeapon weapon)
    {
        var foundWeapon = _weapons.Find(wp => wp.InheritorWeapon.Index == weapon.InheritorWeapon.Index);

        if (foundWeapon == null)
        {
            _weapons.Add(weapon);
            return weapon;
        }

        var index = _weapons.IndexOf(foundWeapon);
        _weapons[index] = foundWeapon;
        return foundWeapon;
    }
}