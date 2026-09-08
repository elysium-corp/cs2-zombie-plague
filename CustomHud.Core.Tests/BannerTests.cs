using CustomHud.Api;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class BannerTests
{
    [Fact]
    public void ExitKeepsContentThenClearsAndReplacementCancelsTheOldExit()
    {
        var clock = new HudMessageStoreTests.Clock();
        var runtime = new Runtime();
        var presenter = new HudPresenter(runtime, clock);
        var first = Message(1);
        presenter.Render(1, 11, HudPosition.TopCenter, first);
        presenter.Render(1, 11, HudPosition.TopCenter, null);
        Assert.True(runtime.Classes["Shown"]);
        Assert.True(runtime.Classes["Leaving"]);
        Assert.Equal("Description", runtime.Texts["Message1Line0Run0"]);
        clock.Advance(0.2);
        presenter.Render(1, 11, HudPosition.TopCenter, null);
        Assert.True(runtime.Classes["Shown"]);
        presenter.Render(1, 11, HudPosition.TopCenter, Message(2));
        Assert.False(runtime.Classes["Leaving"]);
        clock.Advance(1);
        presenter.Render(1, 11, HudPosition.TopCenter, Message(2));
        Assert.True(runtime.Classes["Shown"]);
        presenter.Render(1, 11, HudPosition.TopCenter, null);
        clock.Advance(0.4);
        presenter.Render(1, 11, HudPosition.TopCenter, null);
        Assert.False(runtime.Classes["Shown"]);
        Assert.Empty(runtime.Texts["Message1Line0Run0"]);
    }

    [Fact]
    public void SoundWaitsForVisibilityAndDoesNotRepeatOnResumeRecoveryOrUnchangedFrame()
    {
        var clock = new HudMessageStoreTests.Clock();
        var store = new HudMessageStore(clock);
        var sounds = new List<int>();
        var presenter = new HudPresenter(new Runtime(), clock, (player, _, _) => sounds.Add(player));
        store.Put(1, 11, Message(1).Document, new() { Channel = "ad", DurationSeconds = 10 });
        store.Put(1, 11, HudMarkup.Parse("round", HudTextFormat.PlainText, HudMessageStyle.Notice), new() { Channel = "round", DurationSeconds = 1, Priority = 100 });
        void Render() => presenter.Render(1, 11, HudPosition.TopCenter, store.GetFrame(1, 11)[1]);
        Render();
        Assert.Empty(sounds);
        clock.Advance(1);
        Render(); Render();
        Assert.Single(sounds);
        store.Put(1, 11, HudMarkup.Parse("round", HudTextFormat.PlainText, HudMessageStyle.Notice), new() { Channel = "round", DurationSeconds = 1, Priority = 100 });
        Render(); clock.Advance(1); Render();
        presenter = new HudPresenter(new Runtime(), clock, (player, _, _) => sounds.Add(player));
        Render();
        Assert.Single(sounds);
    }

    [Fact]
    public void DisconnectImmediatelyClearsOutgoingBanner()
    {
        var runtime = new Runtime();
        var presenter = new HudPresenter(runtime, new HudMessageStoreTests.Clock());
        presenter.Render(1, 11, HudPosition.TopCenter, Message(1));
        presenter.Render(1, 11, HudPosition.TopCenter, null);
        presenter.Clear(1);
        Assert.False(runtime.Classes["Shown"]);
        Assert.False(runtime.Classes["Leaving"]);
    }

    [Theory]
    [InlineData("text", false, false)]
    [InlineData("icon", false, false)]
    [InlineData("headline", false, true)]
    [InlineData("feature", true, true)]
    [InlineData("hero", true, true)]
    public void VariantsRequireOnlyTheirFields(string variant, bool header, bool title)
    {
        var template = new HudBannerTemplate { Variant = variant, Icon = variant is "icon" or "feature" ? "info" : "none" };
        var content = new HudBannerContent { Header = header ? "Header" : null, Title = title ? "Title" : null, Description = "Text" };
        HudBannerDesign.Validate(template, content);
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Validate(template, content with { Description = "" }));
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Validate(template with { Sound = "event;exec command" }, content));
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Validate(template with { Volume = float.NaN }, content));
    }

    private static HudMessage Message(long revision) => new(revision, new(), HudBannerDesign.Parse(
        new() { Sound = "ZombiePlagueSounds.round_start_2" }, new() { Title = "Title", Description = "Description" }, HudTextFormat.Markup), 0);

    private sealed class Runtime : IHudRuntime
    {
        internal readonly Dictionary<string, bool> Classes = [];
        internal readonly Dictionary<string, string> Texts = [];
        public bool IsValid => true;
        public void SetClass(int playerId, string panel, string name, bool enabled) { if (panel == "Message1") Classes[name] = enabled; }
        public void SetText(int playerId, string panel, string text) => Texts[panel] = text;
        public void Dispose() { }
    }
}
