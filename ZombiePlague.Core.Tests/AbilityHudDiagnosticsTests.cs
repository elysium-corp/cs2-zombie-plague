using System.Reflection;
using Common.Database.Storages;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;
using ZombiePlague.Core.Hud.AbilityHud;
using ZombiePlague.Core.Store.Data;
using RoleManager = ZombiePlague.Core.Data.Managers.Contracts.IPlayerManager;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityHudDiagnosticsTests
{
    [Fact]
    public void LiveZombieDiagnosticsMatchItsRenderedAbilitiesWithoutActivation()
    {
        var player = Player(1);
        var ability = new ProbeAbility { Presentation = new("heal", "Ability.Heal.Name", "heal") };
        var role = Role(player, [ability]);
        var config = new AbilityHudConfig { ShowNames = false };
        var presenter = new AbilityHudPresenter(new NullSink());
        var frame = AbilityHudFrame.ForPlayer(player, role, config, NoLocalization, out var reason);
        presenter.Render(1, frame);
        Assert.Equal(AbilityHudVisibility.Ready, reason);
        Assert.Equal(1, presenter.PlayerCount);
        Assert.Equal(1, presenter.GetIconCount(1));
        Assert.Equal("heal", Assert.Single(frame.Icons).Key);
        Assert.Equal(0, ability.Uses);
    }

    [Fact]
    public void EmptyZombieClassAndAbilitiesWithoutPresentationHaveDifferentCauses()
    {
        var player = Player(1);
        var abilities = new List<IAbility>();
        var role = Role(player, abilities);
        var config = new AbilityHudConfig { ShowNames = false };
        Assert.Empty(AbilityHudFrame.ForPlayer(player, role, config, NoLocalization, out var reason).Icons);
        Assert.Equal(AbilityHudVisibility.NoAbilities, reason);

        var ability = new ProbeAbility();
        abilities.Add(ability);
        Assert.Empty(AbilityHudFrame.ForPlayer(player, role, config, NoLocalization, out reason).Icons);
        Assert.Equal(AbilityHudVisibility.MissingPresentation, reason);

        ability.Presentation = new("heal", "Ability.Heal.Name", "heal");
        Assert.Single(AbilityHudFrame.ForPlayer(player, role, config, NoLocalization, out reason).Icons);
        Assert.Equal(AbilityHudVisibility.Ready, reason);
        Assert.Equal(0, ability.Uses);
    }

    [Fact]
    public void DeathOrMissingRoleClearsRecipients()
    {
        var player = Player(1);
        var role = Role(player, [new ProbeAbility { Presentation = new("heal", "Heal", "heal") }]);
        var config = new AbilityHudConfig { ShowNames = false };
        var presenter = new AbilityHudPresenter(new NullSink());
        presenter.Render(1, AbilityHudFrame.ForPlayer(player, role, config, NoLocalization, out var reason));
        Assert.Equal(AbilityHudVisibility.Ready, reason);
        Assert.Equal(1, presenter.PlayerCount);

        presenter.Render(1, AbilityHudFrame.ForPlayer(Player(1, alive: false), role, config, NoLocalization, out reason));
        Assert.Equal(AbilityHudVisibility.Dead, reason);
        Assert.Equal(0, presenter.GetIconCount(1));

        presenter.Render(1, AbilityHudFrame.ForPlayer(player, null, config, NoLocalization, out reason));
        Assert.Equal(AbilityHudVisibility.MissingRole, reason);
        Assert.Equal(0, presenter.PlayerCount);
    }

    [Fact]
    public void ServerDebugIncludesConnectedPlayersWithoutRolesAndDoesNotChangeAbilitiesOrStartHud()
    {
        using var fixture = new CommandFixture();
        var output = fixture.Execute("debug");
        Assert.Contains(output, line => line.Contains("connected=2; tracked=1; recipients=0; ticks=0; last_tick_ms=never"));
        Assert.Contains(output, line => line.Contains("slot=1;") && line.Contains("role=zombie; class=zombie_cleric"));
        Assert.Contains(output, line => line.Contains("slot=1; reason=Ready;") && line.Contains("eligible_icons=1; sent_icons=0"));
        Assert.Contains(output, line => line.Contains("slot=2; reason=MissingRole;"));
        Assert.Contains(output, line => line.Contains("ids=[heal]"));
        Assert.Contains(output, line => line.Contains("icons=[heal:Kind_heal:Ready]"));
        Assert.Equal(0, fixture.Ability.Uses);
        Assert.False(fixture.Service.IsRunning);
        Assert.Empty(fixture.ClientConsole);
    }

    [Fact]
    public void ChatDebugSendsOnlyTheCallersDetailsToConsoleAndKeepsTheChatReplyShort()
    {
        using var fixture = new CommandFixture();
        var chat = fixture.Execute("debug", fixture.Client);
        Assert.Single(chat);
        Assert.Contains("консоль", chat[0]);
        Assert.Contains(fixture.ClientConsole, line => line.Contains("slot=1; reason=Ready;"));
        Assert.DoesNotContain(fixture.ClientConsole, line => line.Contains("slot=2;"));
        Assert.Equal(0, fixture.Ability.Uses);
        Assert.False(fixture.Service.IsRunning);
    }

    private static string NoLocalization(string key) => throw new InvalidOperationException("Подписи выключены");

    private static IZombie Role(IPlayer player, List<IAbility> abilities)
    {
        var document = ZombieCatalogDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));
        return Zombie.Create(null!, player, new ZCatalogClass(document.Classes.Single(item => item.InternalName == "zombie_cleric"), abilities));
    }

    private static IPlayer Player(int id, bool alive = true, List<string>? console = null) => Stub<IPlayer>((method, args) =>
    {
        if (method.Name == "SendMessage")
        {
            Assert.Equal(MessageType.Console, args![0]);
            console!.Add((string)args[1]!);
            return null;
        }
        return method.Name switch
        {
            "get_IsValid" => true,
            "get_IsFakeClient" => false,
            "get_IsAlive" => alive,
            "get_PlayerID" => id,
            "get_SteamID" => 76561198000000000UL + (ulong)id,
            _ => throw new InvalidOperationException(method.Name)
        };
    });

    private sealed class CommandFixture : IDisposable
    {
        public ProbeAbility Ability { get; } = new() { Presentation = new("heal", "Heal", "heal") };
        public List<string> ClientConsole { get; } = [];
        public IPlayer Client { get; }
        public AbilityHudService Service { get; }
        private ICommandService.CommandListener _command = null!;

        public CommandFixture()
        {
            Client = Player(1, console: ClientConsole);
            // Объект владельца роли может отличаться от обёртки из списка подключённых клиентов
            var owner = Player(1);
            var connected = new List<IPlayer> { Client, Player(2) };
            var role = Role(owner, [Ability]);
            var roles = Stub<RoleManager>((method, args) =>
            {
                if (method.Name == "GetAllPlayers") return new[] { owner };
                if (method.Name == "TryGetRole")
                {
                    var found = ReferenceEquals(args![0], owner);
                    args[1] = found ? role : null;
                    return found;
                }
                throw new InvalidOperationException(method.Name);
            });
            var commands = Stub<ICommandService>((method, args) =>
            {
                if (method.Name == "RegisterCommand")
                {
                    Assert.Equal("zp_ability_hud", args![0]);
                    Assert.Equal("zombie_plague.admin.classes", args[3]);
                    _command = (ICommandService.CommandListener)args[1]!;
                    return Guid.NewGuid();
                }
                if (method.Name == "UnregisterCommand") return null;
                throw new InvalidOperationException(method.Name);
            });
            var menus = Stub<IMenuManagerAPI>((method, _) => method.Name == "GetCurrentMenu"
                ? null : throw new InvalidOperationException(method.Name));
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_Command" => commands,
                "get_MenusAPI" => menus,
                "get_PlayerManager" => Stub(method.ReturnType, (member, _) => member.Name == "GetAllPlayers"
                    ? connected : throw new InvalidOperationException(member.Name)),
                "get_Event" => Stub(method.ReturnType, (member, _) => member.Name.StartsWith("add_", StringComparison.Ordinal)
                    || member.Name.StartsWith("remove_", StringComparison.Ordinal) ? null : throw new InvalidOperationException(member.Name)),
                _ => throw new InvalidOperationException(method.Name)
            });
            Service = new(core, roles, Options.Create(new AbilityHudConfig { Enabled = false, ShowNames = false }),
                () => throw new InvalidOperationException("Диагностика не должна обращаться к БД или запускать HUD"),
                new AbilityHudSettings(new PlayerSessionStore<PlayerPreferences>()),
                () => throw new InvalidOperationException("Диагностика не должна создавать HUD"));
            Service.Start();
        }

        public List<string> Execute(string command, IPlayer? sender = null)
        {
            var replies = new List<string>();
            var context = Stub<ICommandContext>((method, args) =>
            {
                if (method.Name == "Reply") { replies.Add((string)args![0]!); return null; }
                return method.Name switch
                {
                    "get_Args" => new[] { command },
                    "get_IsSentByPlayer" => sender is not null,
                    "get_Sender" => sender,
                    _ => throw new InvalidOperationException(method.Name)
                };
            });
            _command(context);
            return replies;
        }

        public void Dispose() => Service.Dispose();
    }

    private sealed class ProbeAbility() : BaseActiveAbility(null!, new HealConfig(), () => throw new InvalidOperationException())
    {
        public int Uses { get; private set; }
        public override KeyKind? Key => KeyKind.E;
        public override float Cooldown => 10;
        public override void Use() => Uses++;
    }

    private sealed class NullSink : IAbilityHudSink
    {
        public void SetClass(int playerId, string panel, string name, bool enabled) { }
        public void SetText(int playerId, string panel, string value) { }
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class => (T)Stub(typeof(T), handler);
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
