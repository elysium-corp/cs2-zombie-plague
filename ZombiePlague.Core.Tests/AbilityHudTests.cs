using System.Text.Json;
using System.Xml.Linq;
using SwiftlyS2.Shared.Events;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Experimental.AbilityHud;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityHudTests
{
    [Fact]
    public void CatalogIdentitySurvivesReloadAndDifferentIdsOfSameMechanicStaySeparate()
    {
        var definition = new ZombieAbilityDefinition
        {
            InternalName = "elite_heal", DisplayName = "Лечение элиты", Kind = "heal",
            Parameters = JsonSerializer.SerializeToElement(new { HealAmount = 2000 })
        };
        var factory = new AbilityFactory(null!, null!, () => throw new InvalidOperationException());
        var first = factory.Create(definition);
        definition.DisplayName = "Новое имя после reload";
        definition.InternalName = "other_heal";
        var second = factory.Create(definition);
        var frame = AbilityHudFrame.Create([first, first, second], 8, false, false);
        Assert.Equal(2, frame.Icons.Length);
        Assert.Equal("Лечение элиты", frame.Icons[0].Name);
        Assert.Equal("elite_heal", frame.Icons[0].Key);
        Assert.Equal("other_heal", frame.Icons[1].Key);
        Assert.All(frame.Icons, icon => Assert.Equal("heal", icon.Kind));
    }

    [Fact]
    public void FrameReadsActualCooldownWithoutUsingOrMutatingTheAbility()
    {
        var ability = new ProbeAbility { Presentation = new("heal", "Лечение", "heal"), IsActive = true };
        var frame = AbilityHudFrame.Create([ability], 8, false, false);
        Assert.Equal("Cooling", frame.Icons[0].State);
        Assert.Equal("13", frame.Icons[0].Countdown);
        Assert.Equal("E", frame.Icons[0].Hotkey);
        Assert.True(ability.IsActive);
        Assert.Equal(0, ability.Uses);
        ability.ResetCooldown();
        frame = AbilityHudFrame.Create([ability], 8, false, false);
        Assert.Equal("Ready", frame.Icons[0].State);
        Assert.Empty(frame.Icons[0].Countdown);
    }

    [Fact]
    public void PassiveBuffIsStableAndOverflowDoesNotChangeTheOwnedSet()
    {
        var passive = new DoubleJump(null!, new())
        {
            Presentation = new("double_jump", "Двойной прыжок", "double_jump"), IsActive = true
        };
        var active = new ProbeAbility { Presentation = new("leap", "Прыжок", "leap") };
        var frame = AbilityHudFrame.Create([passive, active], 1, true, false);
        Assert.Single(frame.Icons);
        Assert.Equal(1, frame.Overflow);
        Assert.Equal("Passive", frame.Icons[0].State);
        Assert.Empty(frame.Icons[0].Countdown);
        Assert.Empty(frame.Icons[0].Hotkey);
        Assert.Equal("CTRL+SPACE", AbilityHudFrame.Create([active], 12, true, true).Icons[0].Hotkey);
        Assert.True(passive.IsActive);
    }

    [Fact]
    public void PresenterSendsOnlyChangesAndKeepsPlayersIndependent()
    {
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        var first = new AbilityHudFrame([Icon("heal", "Ready", "")], 0, false, false);
        presenter.Render(1, first);
        Assert.Equal("C:1:AbilityBuffs:Shown:True", sink.Calls[^1]);
        sink.Calls.Clear();
        presenter.Render(1, first with { Icons = [Icon("heal", "Ready", "")] });
        Assert.Empty(sink.Calls);
        presenter.Render(2, first);
        Assert.All(sink.Calls, value => Assert.Contains(":2:", value));
        sink.Calls.Clear();
        presenter.Render(1, first with { Icons = [Icon("heal", "Cooling", "10")] });
        Assert.Equal(3, sink.Calls.Count);
        Assert.Contains("C:1:Buff0:Ready:False", sink.Calls);
        Assert.Contains("C:1:Buff0:Cooling:True", sink.Calls);
        Assert.Contains("T:1:Buff0Time:10", sink.Calls);
    }

    [Fact]
    public void RoleChangesAndDisconnectClearOldIconsAndSlotOverrides()
    {
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        presenter.Render(4, new([Icon("heal", "Cooling", "9"), Icon("leap", "Ready", "")], 0, false, true));
        sink.Calls.Clear();
        presenter.Render(4, new([Icon("double_jump", "Passive", "")], 0, true, true));
        Assert.Contains("C:4:Buff0:Kind_heal:False", sink.Calls);
        Assert.Contains("C:4:Buff0:Kind_double_jump:True", sink.Calls);
        Assert.Contains("C:4:Buff1:Shown:False", sink.Calls);
        presenter.Clear(4);
        Assert.DoesNotContain(4, presenter.PlayerIds);
        Assert.Equal("C:4:AbilityBuffs:Shown:False", sink.Calls[^1]);
        sink.Calls.Clear();
        presenter.Render(4, new([Icon("catch", "Ready", "")], 0, false, false));
        Assert.Contains("C:4:Buff0:Kind_catch:True", sink.Calls);
        Assert.Equal("C:4:AbilityBuffs:Shown:True", sink.Calls[^1]);
    }

    [Fact]
    public void HiddenFramesStaySilentUntilThePlayerReturns()
    {
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        presenter.Render(3, AbilityHudFrame.Empty);
        sink.Calls.Clear();
        presenter.Render(3, AbilityHudFrame.Empty);
        Assert.Empty(sink.Calls);
    }

    [Fact]
    public void OldSdkCanLoadExperimentWithoutNativeHudMethods()
    {
        Assert.False(new AbilityHudConfig().Enabled);
        Assert.False(CustomHudRuntime.HasRequiredApi);
    }

    [Fact]
    public void ConfigRejectsUnboundedTimersAndMoreIconsThanTheLayoutSupports()
    {
        Assert.Throws<InvalidDataException>(() => new AbilityHudConfig { RefreshSeconds = 0 }.Validate());
        Assert.Throws<InvalidDataException>(() => new AbilityHudConfig { RefreshSeconds = float.NaN }.Validate());
        Assert.Throws<InvalidDataException>(() => new AbilityHudConfig { MaximumIcons = 13 }.Validate());
    }

    [Fact]
    public void PanoramaHasEveryServerTargetAndEveryIconWithoutScriptsOrInput()
    {
        var content = Path.Combine(AppContext.BaseDirectory, "ability-hud", "content", "panorama");
        var xml = XDocument.Load(Path.Combine(content, "layout", "custom_game", "elysium_ability_buffs.xml"));
        var ids = xml.Descendants().Attributes("id").Select(value => value.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Contains("AbilityBuffs", ids);
        Assert.Contains("Side", ids);
        Assert.Contains("Overflow", ids);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "scripts" or "Button");
        for (var slot = 0; slot < AbilityHudFrame.SlotCount; slot++)
        {
            foreach (var suffix in new[] { "", "Name", "Time", "Key" }) Assert.Contains("Buff" + slot + suffix, ids);
            var panel = xml.Descendants("Panel").Single(node => (string?)node.Attribute("id") == "Buff" + slot);
            Assert.Equal(AbilityHudFrame.Kinds.Length, panel.Descendants("Image").Count());
        }
        foreach (var src in xml.Descendants("Image").Attributes("src").Select(value => value.Value).Distinct())
        {
            var path = Path.Combine(content, "images", src.Replace("file://{images}/", ""));
            Assert.True(File.Exists(path), path);
            var svg = XDocument.Load(path);
            Assert.Equal("svg", svg.Root!.Name.LocalName);
        }
    }

    private static AbilityHudIcon Icon(string kind, string state, string countdown) => new(kind, kind, kind, state, countdown, "");

    private sealed class RecordingSink : IAbilityHudSink
    {
        public List<string> Calls { get; } = [];
        public void SetClass(int playerId, string panel, string name, bool enabled) => Calls.Add($"C:{playerId}:{panel}:{name}:{enabled}");
        public void SetText(int playerId, string panel, string value) => Calls.Add($"T:{playerId}:{panel}:{value}");
    }

    private sealed class ProbeAbility() : BaseActiveAbility(null!, new HealConfig(), () => throw new InvalidOperationException())
    {
        public int Uses { get; private set; }
        public override KeyKind? Key => KeyKind.E;
        public override float Cooldown => 12.3f;
        public override void Use() => Uses++;
    }
}
