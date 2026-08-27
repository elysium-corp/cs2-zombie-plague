using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentPostEvents(IHookSubscriber hooks) : ICustomEquipmentPostEvents
{
    private readonly HookEvent<ItemBuyPostContext> _itemBuy = new(hooks);
    private readonly HookEvent<ItemGivePostContext> _itemGive = new(hooks);
    private readonly HookEvent<WeaponGivePostContext> _weaponGive = new(hooks);
    private readonly HookEvent<GrenadeGivePostContext> _grenadeGive = new(hooks);
    private readonly HookEvent<GrenadeThrowPostContext> _grenadeThrow = new(hooks);
    private readonly HookEvent<GrenadeDetonatePostContext> _grenadeDetonate = new(hooks);
    private readonly HookEvent<MinePlacePostContext> _minePlace = new(hooks);

    public IHookSubscription<ItemBuyPostContext> ItemBuy => _itemBuy;

    public event HookHandler<ItemBuyPostContext> ItemBuyEvent
    {
        add => _itemBuy.Event += value;
        remove => _itemBuy.Event -= value;
    }

    public IHookSubscription<ItemGivePostContext> ItemGive => _itemGive;

    public event HookHandler<ItemGivePostContext> ItemGiveEvent
    {
        add => _itemGive.Event += value;
        remove => _itemGive.Event -= value;
    }

    public IHookSubscription<WeaponGivePostContext> WeaponGive => _weaponGive;

    public event HookHandler<WeaponGivePostContext> WeaponGiveEvent
    {
        add => _weaponGive.Event += value;
        remove => _weaponGive.Event -= value;
    }

    public IHookSubscription<GrenadeGivePostContext> GrenadeGive => _grenadeGive;

    public event HookHandler<GrenadeGivePostContext> GrenadeGiveEvent
    {
        add => _grenadeGive.Event += value;
        remove => _grenadeGive.Event -= value;
    }

    public IHookSubscription<GrenadeThrowPostContext> GrenadeThrow => _grenadeThrow;

    public event HookHandler<GrenadeThrowPostContext> GrenadeThrowEvent
    {
        add => _grenadeThrow.Event += value;
        remove => _grenadeThrow.Event -= value;
    }

    public IHookSubscription<GrenadeDetonatePostContext> GrenadeDetonate => _grenadeDetonate;

    public event HookHandler<GrenadeDetonatePostContext> GrenadeDetonateEvent
    {
        add => _grenadeDetonate.Event += value;
        remove => _grenadeDetonate.Event -= value;
    }

    public IHookSubscription<MinePlacePostContext> MinePlace => _minePlace;

    public event HookHandler<MinePlacePostContext> MinePlaceEvent
    {
        add => _minePlace.Event += value;
        remove => _minePlace.Event -= value;
    }
}
