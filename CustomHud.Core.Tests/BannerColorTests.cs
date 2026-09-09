using System.Text.RegularExpressions;
using System.Xml.Linq;
using CustomHud.Api;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class BannerColorTests
{
    [Theory]
    [InlineData("inherit", "red", "#FFD36A")]
    [InlineData("white", "red", "#FFFFFF")]
    [InlineData("accent", "red", "#85DCB1")]
    [InlineData("inherit", "white", "#FFD36A")]
    [InlineData("white", "white", "#FFFFFF")]
    [InlineData("accent", "white", "#85DCB1")]
    public void ExplicitParameterColorWinsAndIsRemovedOnNextMessage(string parameterColor, string inlineColor, string fallback)
    {
        var design = new HudBannerTemplate
        {
            Variant = "custom", Theme = "light", WidthPixels = 960,
            HeaderColor = "gold", TitleColor = "gold", DescriptionColor = "gold",
            ParameterColor = parameterColor, Accent = "mint"
        };
        var text = $"До заражения <font color='{inlineColor}'><b><span class='hud-parameter'>10</span></b></font> сек. <span class='hud-parameter'>20</span>";
        using var runtime = new LayoutRuntime();
        var presenter = new HudPresenter(runtime);
        var document = HudBannerDesign.Parse(design, new() { Header = text, Title = text, Description = text }, HudTextFormat.Markup);
        presenter.Render(1, 1, HudPosition.TopLeft, new(1, new(), document, 0));
        Assert.True(HudPalette.TryResolve(inlineColor, out var color));
        foreach (var field in new[] { "Header", "Title", "Line0" })
        {
            var prefix = "Message0" + field + "Run";
            Assert.Equal("#FFD36A", runtime.Color(prefix + "0"));
            Assert.Equal($"#{HudPalette.Colors[color]:X6}", runtime.Color(prefix + "1"));
            Assert.Contains("Bold", runtime.Classes(prefix + "1"));
            Assert.Contains("Parameter", runtime.Classes(prefix + "1"));
            Assert.Equal("#FFD36A", runtime.Color(prefix + "2"));
            Assert.Equal(fallback, runtime.Color(prefix + "3"));
        }

        // Следующее обновление того же баннера снимает старые HTML-классы.
        var plain = "До заражения <span class='hud-parameter'>9</span> сек.";
        document = HudBannerDesign.Parse(design, new() { Header = plain, Title = plain, Description = plain }, HudTextFormat.Markup);
        presenter.Render(1, 1, HudPosition.TopLeft, new(2, new(), document, 0));
        Assert.Equal(fallback, runtime.Color("Message0Line0Run1"));
        Assert.DoesNotContain("Bold", runtime.Classes("Message0Line0Run1"));
    }

    // Применяет команды настоящего presenter к поставляемому XML и вычисляет
    // приоритет простых CSS-правил color по специфичности и порядку в файле.
    // Это проверка каскада ресурсов, а не замена компиляции в Workshop Tools.
    private sealed class LayoutRuntime : IHudRuntime
    {
        private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "content/panorama");
        private readonly Dictionary<string, XElement> _panels = XDocument.Load(Path.Combine(Root,
            "layout/custom_game/elysium_messages_v4_r3.xml")).Descendants()
            .Where(element => element.Attribute("id") is not null).ToDictionary(element => element.Attribute("id")!.Value);
        private readonly string _css = File.ReadAllText(Path.Combine(Root, "styles/custom_game/elysium_messages_v4_r3.css"));
        public bool IsValid => true;
        internal HashSet<string> Classes(string panel) => Classes(_panels[panel]);
        private static HashSet<string> Classes(XElement panel) => (panel.Attribute("class")?.Value ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        public void SetClass(int playerId, string panel, string name, bool enabled)
        {
            var classes = Classes(panel);
            if (enabled) classes.Add(name); else classes.Remove(name);
            _panels[panel].SetAttributeValue("class", string.Join(' ', classes));
        }
        public void SetText(int playerId, string panel, string text) => _panels[panel].SetAttributeValue("text", text);
        public void Dispose() { }

        internal string Color(string panel)
        {
            string? color = null;
            var specificity = -1;
            foreach (Match rule in Regex.Matches(_css, @"([^{}]+)\{([^{}]*)\}"))
            {
                var value = Regex.Match(rule.Groups[2].Value, @"(?:^|;)\s*color:\s*(#[A-Fa-f0-9]{6})\s*;");
                if (!value.Success) continue;
                foreach (var selector in rule.Groups[1].Value.Split(','))
                {
                    var groups = selector.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (groups.Length == 0 || groups.Any(group => !Regex.IsMatch(group, @"^(\.[A-Za-z_][A-Za-z_0-9]*)+$"))) continue;
                    var weight = groups.Sum(group => group.Count(character => character == '.'));
                    if (weight < specificity || !Matches(_panels[panel], groups)) continue;
                    specificity = weight;
                    color = value.Groups[1].Value.ToUpperInvariant();
                }
            }
            Assert.NotNull(color);
            return color;
        }

        private static bool Matches(XElement panel, string[] groups)
        {
            static bool HasClasses(XElement element, string group) => group.Split('.', StringSplitOptions.RemoveEmptyEntries).All(Classes(element).Contains);
            if (!HasClasses(panel, groups[^1])) return false;
            var ancestor = panel.Parent;
            for (var index = groups.Length - 2; index >= 0; index--)
            {
                while (ancestor is not null && !HasClasses(ancestor, groups[index])) ancestor = ancestor.Parent;
                if (ancestor is null) return false;
                ancestor = ancestor.Parent;
            }
            return true;
        }
    }
}
