using System.Reflection;
using Common.Hooks;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Giver;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentPickupTests
{
    [Fact]
    public void PickupRestoresExistingInstanceForNewOwnerWithoutGrantingAnotherItem()
    {
        using var fixture = new Fixture();
        fixture.Pickup();
        fixture.Equip();
        Assert.Single(fixture.Updates);
        fixture.Flush();

        Assert.Equal(1, fixture.Item.Reapplications);
        Assert.Equal(1, fixture.Grants);
        Assert.Same(fixture.Weapon, fixture.Item.AttachedEntity);
        Assert.Single(fixture.Inventory);
    }

    [Fact]
    public void EquippingAgainRestoresCustomizationAndIgnoresUntrackedWeapons()
    {
        using var fixture = new Fixture();
        fixture.Equip();
        fixture.Flush();
        fixture.Equip();
        fixture.Flush();
        Assert.Equal(2, fixture.Item.Reapplications);

        fixture.Inventory.Clear();
        fixture.Inventory.Add(Weapon(99));
        fixture.Equip();
        fixture.Flush();
        Assert.Equal(2, fixture.Item.Reapplications);
    }

    [Theory]
    [InlineData("dropped")]
    [InlineData("dead")]
    [InlineData("disconnected")]
    [InlineData("new_pawn")]
    [InlineData("infected")]
    [InlineData("removed_from_catalog")]
    [InlineData("unloaded")]
    public void PendingPickupDoesNotRestoreInvalidOrUnavailableItem(string change)
    {
        using var fixture = new Fixture();
        fixture.Pickup();
        switch (change)
        {
            case "dropped": fixture.Inventory.Clear(); break;
            case "dead": fixture.Alive = false; break;
            case "disconnected": fixture.Connected = false; break;
            case "new_pawn": fixture.PawnAddress++; break;
            case "infected": fixture.Infected = true; break;
            case "removed_from_catalog": fixture.Registered = false; break;
            case "unloaded": fixture.Service.Dispose(); break;
        }
        fixture.Flush();
        Assert.Equal(0, fixture.Item.Reapplications);
    }

    private sealed class Fixture : IDisposable
    {
        public bool Alive { get; set; } = true;
        public bool Connected { get; set; } = true;
        public bool Infected { get; set; }
        public bool Registered { get; set; } = true;
        public nint PawnAddress { get; set; } = 100;
        public Queue<Action> Updates { get; } = new();
        public List<CBasePlayerWeapon> Inventory { get; } = [];
        public CCSWeaponBase Weapon { get; } = EquipmentPickupTests.Weapon(10);
        public ProbeItem Item { get; }
        public int Grants { get; private set; }
        public EquipmentService Service { get; }
        private readonly Dictionary<Type, Delegate> _gameHandlers = [];
        private readonly IPlayer _recipient;

        public Fixture()
        {
            Item = new ProbeItem { AttachedEntity = Weapon };
            var pawn = Stub<CCSPlayerPawn>((method, _) => method.Name switch
            {
                "get_IsValid" => true,
                "get_Address" => PawnAddress,
                "get_WeaponServices" => Stub(method.ReturnType, (member, _) =>
                    member.Name == "get_MyValidWeapons" ? Inventory
                        : throw new InvalidOperationException(member.Name)),
                _ => throw new InvalidOperationException(method.Name)
            });
            _recipient = Stub<IPlayer>((method, _) => method.Name switch
            {
                "get_IsValid" => true,
                "get_IsAlive" => Alive,
                "get_PlayerPawn" => pawn,
                "get_SessionId" => 2UL,
                _ => throw new InvalidOperationException(method.Name)
            });
            var buyer = Stub<IPlayer>((method, _) => method.Name == "get_IsValid"
                ? true : throw new InvalidOperationException(method.Name));
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_Event" or "get_GameHooks" => Stub(method.ReturnType, NoopHooks),
                "get_GameEvent" => Stub(method.ReturnType, (member, args) =>
                {
                    if (member.Name == "Unhook") return null;
                    Assert.Equal("HookPost", member.Name);
                    _gameHandlers.Add(member.GetGenericArguments()[0], (Delegate)args![0]!);
                    return Guid.NewGuid();
                }),
                "get_Scheduler" => Stub(method.ReturnType, (_, args) =>
                {
                    Updates.Enqueue((Action)args![0]!);
                    return null;
                }),
                "get_PlayerManager" => Stub(method.ReturnType, (_, args) =>
                {
                    Assert.Equal(2UL, args![0]);
                    return Connected ? _recipient : null;
                }),
                "get_EntitySystem" => Stub(method.ReturnType, (member, _) =>
                    member.Name == "GetAllEntities" ? Array.Empty<CEntityInstance>()
                        : throw new InvalidOperationException(member.Name)),
                _ => throw new InvalidOperationException(method.Name)
            });
            var registry = Stub<IItemRegistry>((method, args) =>
            {
                if (method.Name == "Create") return Item;
                Assert.Equal("TryGetDefinition", method.Name);
                args![1] = Registered ? Item : null;
                return Registered;
            });
            var giver = Stub<IItemGiver>((method, args) =>
            {
                Assert.Equal("GiveItem", method.Name);
                Assert.Same(buyer, args![0]);
                Grants++;
                ((Action<ItemBase>)args[3]!)(Item);
                return null;
            });
            var zombies = Stub<IZombiePlagueApi>((method, _) => method.Name switch
            {
                "IsInfected" => Infected,
                "get_Events" => Stub(method.ReturnType, NoopHooks),
                _ => throw new InvalidOperationException(method.Name)
            });
            Service = new(core, giver, registry, new HookService(), () => zombies);
            Service.Initialize();
            Flush();
            Assert.True(Service.TryGiveItem(buyer, Item.InternalName));
            // Тот же экземпляр теперь находится у другого игрока.
            Inventory.Add(Weapon);
        }

        public void Pickup() => _gameHandlers[typeof(EventItemPickup)].DynamicInvoke(
            Stub<EventItemPickup>((_, _) => _recipient));
        public void Equip() => _gameHandlers[typeof(EventItemEquip)].DynamicInvoke(
            Stub<EventItemEquip>((_, _) => _recipient));
        public void Flush()
        {
            while (Updates.TryDequeue(out var callback)) callback();
        }
        public void Dispose() => Service.Dispose();
    }

    private sealed class ProbeItem : ItemBase
    {
        public int Reapplications { get; private set; }
        public override AccessFlags AccessFlags => AccessFlags.Human;
        public override string DisplayName => "Special weapon";
        public override string InternalName => "special_weapon";
        public override string SubclassName => "custom_weapon";
        public override Slot Slot => Slot.Primary;
        public override string Model => "models/special_weapon.vmdl";
        public override void ReapplyCustomization() => Reapplications++;
    }

    private static CCSWeaponBase Weapon(uint index) => Stub<CCSWeaponBase>((method, _) => method.Name switch
    {
        "get_Index" => index,
        "get_IsValid" => true,
        _ => throw new InvalidOperationException(method.Name)
    });

    private static object? NoopHooks(MethodInfo method, object?[]? _) =>
        method.Name.StartsWith("get_", StringComparison.Ordinal)
            ? Stub(method.ReturnType, NoopHooks) : null;

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class =>
        (T)Stub(typeof(T), handler);

    private static object Stub(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create(type, typeof(InterfaceStub));
        ((InterfaceStub)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
