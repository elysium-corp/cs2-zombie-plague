using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Core.Validation;

namespace Menu.Core.Tests;

public sealed class MenuReleaseValidatorTests
{
    private readonly MenuReleaseValidator _validator = new();

    [Fact]
    public void Validate_RejectsDraftRevisionFromRuntimeRelease()
    {
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(status: MenuLifecycleStatus.Draft)]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "menu.not_published");
    }

    [Fact]
    public void Validate_RejectsC4WhenTargetCapabilityIsDisabled()
    {
        var c4 = TestReleaseFactory.TextItem("bomb", kind: MenuItemKind.C4);
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [c4])]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue =>
            issue.Code == "feature.unsupported" && issue.Message.Contains("c4", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsUnknownProvider()
    {
        var item = TestReleaseFactory.TextItem(
            "provider-menu",
            TestReleaseFactory.OpenProviderMenu("missing-provider", "store"));
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [item])]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "provider.missing");
    }

    [Fact]
    public void Validate_RejectsMissingExportOfKnownProvider()
    {
        var provider = new ProviderValidationEntry(
            "economy",
            MenuContractVersions.MenuCoreApiVersion,
            ProviderAvailability.Online,
            ["wallet"],
            [],
            new Dictionary<string, ProviderArgumentValidator>());
        var item = TestReleaseFactory.TextItem(
            "provider-menu",
            TestReleaseFactory.OpenProviderMenu("economy", "store"));
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [item])]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(new ProviderValidationCatalog([provider])));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "provider.menu_missing");
    }

    [Fact]
    public void Validate_KeepsKnownOfflineProviderAsWarning()
    {
        var provider = new ProviderValidationEntry(
            "economy",
            MenuContractVersions.MenuCoreApiVersion,
            ProviderAvailability.Offline,
            ["store"],
            [],
            new Dictionary<string, ProviderArgumentValidator>());
        var item = TestReleaseFactory.TextItem(
            "provider-menu",
            TestReleaseFactory.OpenProviderMenu("economy", "store"));
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [item])]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(new ProviderValidationCatalog([provider])));

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, issue => issue.Code == "provider.offline");
    }

    [Fact]
    public void Validate_UsesPersistableProviderActionSchemaWhenProviderIsOffline()
    {
        var provider = new ProviderValidationEntry(
            "equipment",
            MenuContractVersions.MenuCoreApiVersion,
            ProviderAvailability.Offline,
            [],
            ["select_weapon"],
            argumentSchemas: new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["select_weapon"] = TestReleaseFactory.Json(new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "weapon" },
                    properties = new { weapon = new { type = "string" } },
                }),
            });
        var action = new MenuActionDefinition
        {
            Kind = MenuActionKind.ProviderAction,
            ProviderKey = "equipment",
            ProviderActionKey = "select_weapon",
            Arguments = TestReleaseFactory.Json(new { weapon = 42 }),
        };
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [TestReleaseFactory.TextItem("select", action)])]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(new ProviderValidationCatalog([provider])));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "provider.argument_type");
    }

    [Fact]
    public void Validate_RejectsScalarProviderActionArguments()
    {
        var provider = new ProviderValidationEntry(
            "settings",
            MenuContractVersions.MenuCoreApiVersion,
            ProviderAvailability.Offline,
            [],
            ["save"],
            argumentSchemas: new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["save"] = TestReleaseFactory.Json(new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new { },
                }),
            });
        var action = new MenuActionDefinition
        {
            Kind = MenuActionKind.ProviderAction,
            ProviderKey = "settings",
            ProviderActionKey = "save",
            Arguments = TestReleaseFactory.Json(42),
        };
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [TestReleaseFactory.TextItem("save", action)])]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(new ProviderValidationCatalog([provider])));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "action.arguments_invalid");
    }

    [Fact]
    public void Validate_OnChangeSchemaReceivesInitialValueMergedIntoArguments()
    {
        var provider = new ProviderValidationEntry(
            "settings",
            MenuContractVersions.MenuCoreApiVersion,
            ProviderAvailability.Offline,
            [],
            ["set_mode"],
            argumentSchemas: new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["set_mode"] = TestReleaseFactory.Json(new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "scope", "value" },
                    properties = new
                    {
                        scope = new { type = "string" },
                        value = new { type = "string", @enum = new[] { "expected" } },
                    },
                }),
            });
        var item = new MenuItemDefinition
        {
            ItemKey = "mode",
            Kind = MenuItemKind.Choice,
            Text = TestReleaseFactory.Text("Mode"),
            Value = new MenuItemValueDefinition
            {
                Initial = TestReleaseFactory.Json("expected"),
                Choices =
                [
                    new MenuChoiceOptionDefinition
                    {
                        OptionKey = "expected",
                        Text = TestReleaseFactory.Text("Expected"),
                        Value = TestReleaseFactory.Json("expected"),
                    },
                ],
            },
            OnChange = new MenuActionDefinition
            {
                Kind = MenuActionKind.ProviderAction,
                ProviderKey = "settings",
                ProviderActionKey = "set_mode",
                Arguments = TestReleaseFactory.Json(new { scope = "player", value = "stale" }),
            },
        };
        var release = TestReleaseFactory.Release(
            menus: [TestReleaseFactory.Menu(items: [item])]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(new ProviderValidationCatalog([provider])));

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, issue => issue.Code == "provider.offline");
    }

    [Fact]
    public void Validate_RejectsForwardDependencyCycle()
    {
        var first = TestReleaseFactory.Menu(
            "first",
            items: [TestReleaseFactory.TextItem("to-second", TestReleaseFactory.OpenMenu("second"))]);
        var second = TestReleaseFactory.Menu(
            "second",
            items: [TestReleaseFactory.TextItem("to-first", TestReleaseFactory.OpenMenu("first"))]);
        var release = TestReleaseFactory.Release(menus: [first, second]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "dependency.cycle");
    }

    [Fact]
    public void Validate_RejectsParentCycleWithoutTreatingBackActionsAsForwardEdges()
    {
        var first = TestReleaseFactory.Menu(
            "first",
            parent: new MenuReferenceDefinition { MenuKey = "second" },
            items: [TestReleaseFactory.TextItem("back", TestReleaseFactory.Back())]);
        var second = TestReleaseFactory.Menu(
            "second",
            parent: new MenuReferenceDefinition { MenuKey = "first" },
            items: [TestReleaseFactory.TextItem("back", TestReleaseFactory.Back())]);
        var release = TestReleaseFactory.Release(menus: [first, second]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "parent_dependency.cycle");
        Assert.DoesNotContain(result.Issues, issue => issue.Code == "dependency.cycle");
    }

    [Fact]
    public void Validate_RejectsForwardDependencyDepthBeyondRuntimeGuard()
    {
        var first = TestReleaseFactory.Menu(
            "first",
            items: [TestReleaseFactory.TextItem("to-second", TestReleaseFactory.OpenMenu("second"))]);
        var second = TestReleaseFactory.Menu(
            "second",
            items: [TestReleaseFactory.TextItem("to-third", TestReleaseFactory.OpenMenu("third"))]);
        var third = TestReleaseFactory.Menu("third");
        var release = TestReleaseFactory.Release(menus: [first, second, third]);

        var result = _validator.Validate(
            release,
            TestReleaseFactory.Context(maximumNavigationDepth: 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "dependency.depth_exceeded");
    }

    [Fact]
    public void Validate_RejectsNullCollectionsBeforeTheyReachTheAdapter()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Items = null!,
            RequiredFeatures = null!,
            Design = new MenuDesignDefinition
            {
                KeyBindings = null!,
                ExtraButtons = null!,
            },
        };
        var release = TestReleaseFactory.WithChecksum(
            TestReleaseFactory.Release(menus: [menu]) with
            {
                Checksum = null,
                Commands = null!,
            });

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "release.commands_required");
        Assert.Contains(result.Errors, issue => issue.Code == "menu.items_required");
        Assert.Contains(result.Errors, issue => issue.Code == "menu.required_features_required");
        Assert.Contains(result.Errors, issue => issue.Code == "design.key_bindings_required");
        Assert.Contains(result.Errors, issue => issue.Code == "design.extra_buttons_required");
    }

    [Fact]
    public void Validate_RejectsUnknownOrMistypedDesignOptions()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Design = new MenuDesignDefinition
            {
                Options = new Dictionary<string, System.Text.Json.JsonElement>
                {
                    ["unknownOption"] = TestReleaseFactory.Json(true),
                    ["autoCloseSeconds"] = TestReleaseFactory.Json("soon"),
                },
            },
        };
        var release = TestReleaseFactory.Release(menus: [menu]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "design.option_unknown");
        Assert.Contains(result.Errors, issue => issue.Code == "design.auto_close_invalid");
    }

    [Fact]
    public void Validate_InfersCapabilitiesForSupportedDesignOptions()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Design = new MenuDesignDefinition
            {
                OverrideColor = "#abc",
                Options = new Dictionary<string, System.Text.Json.JsonElement>
                {
                    ["soundEnabled"] = TestReleaseFactory.Json(false),
                    ["autoCloseSeconds"] = TestReleaseFactory.Json(5.5),
                    ["scrollStyle"] = TestReleaseFactory.Json("CenterFixed"),
                    ["footerColor"] = TestReleaseFactory.Json("#11223344"),
                },
            },
        };
        var release = TestReleaseFactory.Release(menus: [menu]);
        var features = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [MenuFeatureKeys.OverrideColor] = true,
            [MenuFeatureKeys.SoundToggle] = true,
            [MenuFeatureKeys.AutoClose] = true,
        };

        var result = _validator.Validate(release, TestReleaseFactory.Context(features: features));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsNamedColorThatSwiftlyCannotApply()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Design = new MenuDesignDefinition { OverrideColor = "green" },
        };
        var release = TestReleaseFactory.Release(menus: [menu]);

        var result = _validator.Validate(release, TestReleaseFactory.Context());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "design.color_invalid");
    }

    [Fact]
    public void Validate_RejectsActionOnStandardNavigationBinding()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Design = new MenuDesignDefinition
            {
                KeyBindings =
                [
                    new MenuKeyBindingDefinition
                    {
                        BindingKey = "exit",
                        Button = "Tab",
                        Action = new MenuActionDefinition { Kind = MenuActionKind.Close },
                    },
                ],
            },
        };
        var release = TestReleaseFactory.Release(menus: [menu]);
        var features = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [MenuFeatureKeys.CustomKeyBinds] = true,
        };

        var result = _validator.Validate(release, TestReleaseFactory.Context(features: features));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "design.navigation_binding_action_forbidden");
    }

    [Fact]
    public void Validate_RejectsCustomBindingWithoutAction()
    {
        var menu = TestReleaseFactory.Menu() with
        {
            Design = new MenuDesignDefinition
            {
                KeyBindings =
                [
                    new MenuKeyBindingDefinition
                    {
                        BindingKey = "help",
                        Button = "F1",
                    },
                ],
            },
        };
        var release = TestReleaseFactory.Release(menus: [menu]);
        var features = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [MenuFeatureKeys.CustomKeyBinds] = true,
        };

        var result = _validator.Validate(release, TestReleaseFactory.Context(features: features));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "design.custom_binding_action_required");
    }
}
