using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentPreEvents(IHookSubscriber hooks) : ICustomEquipmentPreEvents
{
    private readonly HookEvent<ItemBuyPreContext> _itemBuy = new(hooks);
    private readonly HookEvent<ItemGivePreContext> _itemGive = new(hooks);
    private readonly HookEvent<WeaponGivePreContext> _weaponGive = new(hooks);
    private readonly HookEvent<GrenadeGivePreContext> _grenadeGive = new(hooks);
    private readonly HookEvent<GrenadeThrowPreContext> _grenadeThrow = new(hooks);
    private readonly HookEvent<GrenadeDetonatePreContext> _grenadeDetonate = new(hooks);
    private readonly HookEvent<MinePlacePreContext> _minePlace = new(hooks);

    public IHookSubscription<ItemBuyPreContext> ItemBuy => _itemBuy;

    public event HookHandler<ItemBuyPreContext> ItemBuyEvent
    {
        add => _itemBuy.Event += value;
        remove => _itemBuy.Event -= value;
    }

    public IHookSubscription<ItemGivePreContext> ItemGive => _itemGive;

    public event HookHandler<ItemGivePreContext> ItemGiveEvent
    {
        add => _itemGive.Event += value;
        remove => _itemGive.Event -= value;
    }

    public IHookSubscription<WeaponGivePreContext> WeaponGive => _weaponGive;

    public event HookHandler<WeaponGivePreContext> WeaponGiveEvent
    {
        add => _weaponGive.Event += value;
        remove => _weaponGive.Event -= value;
    }

    public IHookSubscription<GrenadeGivePreContext> GrenadeGive => _grenadeGive;

    public event HookHandler<GrenadeGivePreContext> GrenadeGiveEvent
    {
        add => _grenadeGive.Event += value;
        remove => _grenadeGive.Event -= value;
    }

    public IHookSubscription<GrenadeThrowPreContext> GrenadeThrow => _grenadeThrow;

    public event HookHandler<GrenadeThrowPreContext> GrenadeThrowEvent
    {
        add => _grenadeThrow.Event += value;
        remove => _grenadeThrow.Event -= value;
    }

    public IHookSubscription<GrenadeDetonatePreContext> GrenadeDetonate => _grenadeDetonate;

    public event HookHandler<GrenadeDetonatePreContext> GrenadeDetonateEvent
    {
        add => _grenadeDetonate.Event += value;
        remove => _grenadeDetonate.Event -= value;
    }

    public IHookSubscription<MinePlacePreContext> MinePlace => _minePlace;

    public event HookHandler<MinePlacePreContext> MinePlaceEvent
    {
        add => _minePlace.Event += value;
        remove => _minePlace.Event -= value;
    }
}
