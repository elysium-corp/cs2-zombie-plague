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
            InternalName = "elite_heal", DisplayNameKey = "Ability.EliteHeal.Name", DisplayName = "Старое текстовое поле", Kind = "heal",
            Parameters = JsonSerializer.SerializeToElement(new { HealAmount = 2000 })
        };
        var factory = new AbilityFactory(null!, null!, () => throw new InvalidOperationException());
        var first = factory.Create(definition);
        definition.DisplayNameKey = "Ability.OtherHeal.Name";
        definition.InternalName = "other_heal";
        var second = factory.Create(definition);
        var frame = AbilityHudFrame.Create([first, first, second], key => key, true);
        Assert.Equal(2, frame.Icons.Length);
        Assert.Equal("Ability.EliteHeal.Name", frame.Icons[0].Name);
        Assert.Equal("elite_heal", frame.Icons[0].Key);
        Assert.Equal("other_heal", frame.Icons[1].Key);
        Assert.All(frame.Icons, icon => Assert.Equal("heal", icon.Kind));
    }

    [Fact]
    public void FrameReadsActualCooldownWithoutUsingOrMutatingTheAbility()
    {
        var ability = new ProbeAbility { Presentation = new("heal", "Лечение", "heal"), IsActive = true };
        var frame = AbilityHudFrame.Create([ability], key => key, false);
        Assert.Equal("Cooling", frame.Icons[0].State);
        Assert.Equal("13", frame.Icons[0].Countdown);
        Assert.Equal("E", frame.Icons[0].Hotkey);
        Assert.True(ability.IsActive);
        Assert.Equal(0, ability.Uses);
        ability.ResetCooldown();
        frame = AbilityHudFrame.Create([ability], key => key, false);
        Assert.Equal("Ready", frame.Icons[0].State);
        Assert.Empty(frame.Icons[0].Countdown);
    }

    [Fact]
    public void PassiveCooldownKeepsPassiveMarkerAndNeverMovesTheIcon()
    {
        var passive = new ProbePassive
        {
            Presentation = new("double_jump", "Двойной прыжок", "double_jump"), IsActive = true
        };
        var active = new Leap(null!, new(), () => throw new InvalidOperationException())
        {
            Presentation = new("leap", "Прыжок", "leap")
        };
        var frame = AbilityHudFrame.Create([passive, active], key => key, true);
        Assert.Equal("CTRL+SPACE", frame.Icons[0].Hotkey);
        Assert.True(frame.Icons[1].Passive);
        Assert.Equal("Cooling", frame.Icons[1].State);
        Assert.Equal("13", frame.Icons[1].Countdown);
        Assert.Equal("∞", frame.Icons[1].Hotkey);
        Assert.True(passive.IsActive);
        Assert.Equal(0, passive.Uses);
        passive.ResetCooldown();
        var ready = AbilityHudFrame.Create([passive, active], key => key, true);
        Assert.Equal(frame.Icons.Select(icon => icon.Key), ready.Icons.Select(icon => icon.Key));
        Assert.True(ready.Icons[1].Passive);
        Assert.Equal("Ready", ready.Icons[1].State);
        Assert.Empty(ready.Icons[1].Countdown);
    }

    [Fact]
    public void NamesFollowEachPlayersLocalizationAndRefreshWithoutCatalogReload()
    {
        var ability = new ProbeAbility { Presentation = new("heal", "Ability.Heal.Name", "heal") };
        var russian = "Исцеление";
        string Russian(string key) { Assert.Equal("Ability.Heal.Name", key); return russian; }
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        presenter.Render(1, AbilityHudFrame.Create([ability], Russian, true));
        presenter.Render(2, AbilityHudFrame.Create([ability], _ => "Heal", true));
        Assert.Contains("T:1:Buff0Name:Исцеление", sink.Calls);
        Assert.Contains("T:2:Buff0Name:Heal", sink.Calls);
        russian = "Восстановление";
        sink.Calls.Clear();
        presenter.Render(1, AbilityHudFrame.Create([ability], Russian, true));
        Assert.Equal(["T:1:Buff0Name:Восстановление"], sink.Calls);
        Assert.Empty(AbilityHudFrame.Create([ability], _ => throw new InvalidOperationException(), false).Icons[0].Name);
    }

    [Fact]
    public void PassiveAndCoolingClassesCoexistAndPassiveIsRemovedWhenSlotChanges()
    {
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        presenter.Render(1, new([Icon("double_jump", "Cooling", "9", true)], true));
        Assert.Contains("C:1:Buff0:Passive:True", sink.Calls);
        Assert.Contains("C:1:Buff0:Cooling:True", sink.Calls);
        sink.Calls.Clear();
        presenter.Render(1, new([Icon("charge", "Cooling", "9")], true));
        Assert.Contains("C:1:Buff0:Passive:False", sink.Calls);
        Assert.DoesNotContain("C:1:Buff0:Cooling:False", sink.Calls);
    }

    [Fact]
    public void PresenterSendsOnlyChangesAndKeepsPlayersIndependent()
    {
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        var first = new AbilityHudFrame([Icon("heal", "Ready", "")], false);
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
        presenter.Render(4, new([Icon("heal", "Cooling", "9"), Icon("leap", "Ready", "")], true));
        sink.Calls.Clear();
        presenter.Render(4, new([Icon("double_jump", "Ready", "", true)], true));
        Assert.Contains("C:4:Buff0:Kind_heal:False", sink.Calls);
        Assert.Contains("C:4:Buff0:Kind_double_jump:True", sink.Calls);
        Assert.Contains("C:4:Buff1:Shown:False", sink.Calls);
        presenter.Clear(4);
        Assert.DoesNotContain(4, presenter.PlayerIds);
        Assert.Equal("C:4:AbilityBuffs:Shown:False", sink.Calls[^1]);
        sink.Calls.Clear();
        presenter.Render(4, new([Icon("catch", "Ready", "")], false));
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
    public void PinnedSdkContainsHudApiAndExperimentRemainsOptIn()
    {
        Assert.False(new AbilityHudConfig().Enabled);
        Assert.True(CustomHudRuntime.HasRequiredApi);
    }

    [Fact]
    public void ConfigRejectsUnboundedTimers()
    {
        Assert.Throws<InvalidDataException>(() => new AbilityHudConfig { RefreshSeconds = 0 }.Validate());
        Assert.Throws<InvalidDataException>(() => new AbilityHudConfig { RefreshSeconds = float.NaN }.Validate());
    }

    [Fact]
    public void PreflightReportsMissingStyleAndImagesEvenWhenLayoutExists()
    {
        var missing = CustomHudRuntime.MissingResources(path => path == CustomHudRuntime.CompiledLayout);
        Assert.Contains(CustomHudRuntime.CompiledStyle, missing);
        Assert.Equal(9, missing.Length);
        Assert.All(missing.Skip(1), path => Assert.EndsWith(".vsvg_c", path));
        Assert.Empty(CustomHudRuntime.MissingResources(_ => true));
        Assert.Contains(CustomHudRuntime.CompiledLayout, CustomHudRuntime.MissingResources(_ => false));
    }

    [Fact]
    public void PanoramaHasEveryServerTargetAndEveryIconWithoutScriptsOrInput()
    {
        var content = Path.Combine(AppContext.BaseDirectory, "ability-hud", "content", "panorama");
        var xml = XDocument.Load(Path.Combine(content, "layout", "custom_game", "elysium_ability_buffs_v2.xml"));
        var ids = xml.Descendants().Attributes("id").Select(value => value.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Contains("AbilityBuffs", ids);
        Assert.DoesNotContain("Side", ids);
        Assert.DoesNotContain("Overflow", ids);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "scripts" or "Button");
        Assert.Contains("s2r://" + CustomHudRuntime.CompiledStyle, xml.Descendants("include").Attributes("src").Select(value => value.Value));
        for (var row = 0; row < AbilityHudFrame.MaximumRows; row++) Assert.Contains("BuffRow" + row, ids);
        for (var slot = 0; slot < AbilityHudFrame.SlotCount; slot++)
        {
            foreach (var suffix in new[] { "", "Name", "Time", "Key" }) Assert.Contains("Buff" + slot + suffix, ids);
            var panel = xml.Descendants("Panel").Single(node => (string?)node.Attribute("id") == "Buff" + slot);
            Assert.Equal("BuffRow" + slot / AbilityHudFrame.IconsPerRow, (string?)panel.Parent?.Attribute("id"));
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

    private static AbilityHudIcon Icon(string kind, string state, string countdown, bool passive = false) => new(kind, kind, kind, passive, state, countdown, passive ? "∞" : "E");

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

    private sealed class ProbePassive() : BasePassiveAbility(null!, new DoubleJumpConfig())
    {
        public int Uses { get; private set; }
        public override float Cooldown => 12.3f;
        public override void Use() => Uses++;
    }
}
