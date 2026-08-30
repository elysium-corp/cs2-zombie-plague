using System.Text;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Core.Validation;

namespace Menu.Core.Tests;

public sealed class MenuIdentifierAndScopeTests
{
    [Fact]
    public void CanonicalizeAlias_UsesTrimLowercaseAndUnicodeNfc()
    {
        var decomposed = "  /CAF\u0065\u0301  ";

        var canonical = MenuIdentifier.CanonicalizeAlias(decomposed);

        Assert.Equal("/café", canonical);
        Assert.Equal(canonical, canonical.Normalize(NormalizationForm.FormC));
    }

    [Fact]
    public void Validator_RejectsCanonicallyDuplicateUnicodeAliases()
    {
        var release = TestReleaseFactory.Release(
            commands:
            [
                TestReleaseFactory.Command("cafe-decomposed", "/cafe\u0301"),
                TestReleaseFactory.Command("cafe-composed", "/café")
            ]);

        var result = new MenuReleaseValidator().Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "command.alias_duplicate");
        Assert.Contains(result.Warnings, issue => issue.Code == "command.alias_normalized");
    }

    [Fact]
    public void Validator_RejectsReservedConsoleAlias()
    {
        var release = TestReleaseFactory.Release(
            commands:
            [
                TestReleaseFactory.Command(
                    "reserved",
                    "sw_menu_reload",
                    MenuCommandKind.Console)
            ]);

        var result = new MenuReleaseValidator().Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "command.reserved");
    }

    [Fact]
    public void Validator_RejectsAliasCollisionAcrossOverlappingScopes()
    {
        var release = TestReleaseFactory.Release(
            commands:
            [
                TestReleaseFactory.Command("global", "/shop"),
                TestReleaseFactory.Command(
                    "group",
                    "/shop",
                    scope: TestReleaseFactory.GroupScope("zombie"))
            ]);

        var result = new MenuReleaseValidator().Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "command.alias_duplicate");
    }

    [Fact]
    public void Validator_AllowsSameAliasInScopeThatDoesNotApplyToCurrentServer()
    {
        var release = TestReleaseFactory.Release(
            commands:
            [
                TestReleaseFactory.Command(
                    "current-server",
                    "/shop",
                    scope: TestReleaseFactory.ServerScope("zombie-1")),
                TestReleaseFactory.Command(
                    "other-server",
                    "/shop",
                    scope: TestReleaseFactory.ServerScope("zombie-2"))
            ]);

        var result = new MenuReleaseValidator().Validate(release, TestReleaseFactory.Context());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(issue => issue.Code)));
        Assert.DoesNotContain(result.Issues, issue => issue.Code == "command.alias_duplicate");
    }

    [Fact]
    public void ScopeMatcher_ValidatesExclusiveScopeFields()
    {
        Assert.True(MenuScopeMatcher.IsStructurallyValid(TestReleaseFactory.GlobalScope()));
        Assert.True(MenuScopeMatcher.IsStructurallyValid(TestReleaseFactory.ServerScope("zombie-1")));
        Assert.True(MenuScopeMatcher.IsStructurallyValid(TestReleaseFactory.GroupScope("zombie")));

        Assert.False(MenuScopeMatcher.IsStructurallyValid(new MenuScopeDefinition
        {
            Kind = MenuScopeKind.Global,
            ServerKey = "zombie-1"
        }));
        Assert.False(MenuScopeMatcher.IsStructurallyValid(new MenuScopeDefinition
        {
            Kind = MenuScopeKind.Server,
            ServerKey = "zombie-1",
            ServerGroupKey = "zombie"
        }));
    }

    [Fact]
    public void ScopeMatcher_AppliesGlobalServerAndGroupPrecisely()
    {
        IReadOnlySet<string> groups = new HashSet<string>(new[] { "zombie" }, StringComparer.Ordinal);

        Assert.True(MenuScopeMatcher.AppliesTo(TestReleaseFactory.GlobalScope(), "zombie-1", groups));
        Assert.True(MenuScopeMatcher.AppliesTo(TestReleaseFactory.ServerScope("zombie-1"), "zombie-1", groups));
        Assert.False(MenuScopeMatcher.AppliesTo(TestReleaseFactory.ServerScope("zombie-2"), "zombie-1", groups));
        Assert.True(MenuScopeMatcher.AppliesTo(TestReleaseFactory.GroupScope("zombie"), "zombie-1", groups));
        Assert.False(MenuScopeMatcher.AppliesTo(TestReleaseFactory.GroupScope("classic"), "zombie-1", groups));
    }
}
