using System.Text.Json;
using System.Text.RegularExpressions;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Results;
using Menu.Core.Providers;
using Menu.Core.Storage;
using Menu.Core.Swiftly;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared.Menus;

namespace Menu.Core.Validation;

internal sealed partial class MenuReleaseValidator
{
    private const int MaximumMenusPerRelease = 2_048;
    private const int MaximumCommandsPerRelease = 8_192;
    private const int MaximumItemsPerMenu = 1_024;
    private const int MaximumChoicesPerItem = 256;
    private const int MaximumDesignControls = 64;
    private const int MaximumMetadataEntries = 256;

    public MenuReleaseValidationResult Validate(
        MenuReleaseDefinition? release,
        MenuReleaseValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            return ValidateCore(release, context);
        }
        catch (Exception exception)
        {
            return new MenuReleaseValidationResult(
            [
                new MenuValidationIssue
                {
                    Severity = MenuValidationSeverity.Error,
                    Code = "release.validation_failed",
                    Message = $"Release validation failed safely: {exception.GetType().Name}.",
                    Path = "$"
                }
            ]);
        }
    }

    private static MenuReleaseValidationResult ValidateCore(
        MenuReleaseDefinition? release,
        MenuReleaseValidationContext context)
    {
        var issues = new List<MenuValidationIssue>();

        if (release is null)
        {
            AddError(issues, "release.required", "Release payload is required.", "$" );
            return new MenuReleaseValidationResult(issues);
        }

        ValidateHeader(release, context, issues);
        if (issues.Any(static issue => issue.Severity == MenuValidationSeverity.Error))
        {
            return new MenuReleaseValidationResult(issues);
        }

        if (release.Menus is null)
        {
            AddError(issues, "release.menus_required", "Menus collection cannot be null.", "$.menus");
        }

        if (release.Commands is null)
        {
            AddError(issues, "release.commands_required", "Commands collection cannot be null.", "$.commands");
        }

        var menus = release.Menus ?? Array.Empty<MenuDefinition>();
        var commands = release.Commands ?? Array.Empty<MenuCommandDefinition>();
        if (menus.Count > MaximumMenusPerRelease || commands.Count > MaximumCommandsPerRelease)
        {
            AddError(
                issues,
                "release.collection_limit_exceeded",
                $"Release exceeds limits ({MaximumMenusPerRelease} menus, {MaximumCommandsPerRelease} commands).",
                "$");
            return new MenuReleaseValidationResult(issues);
        }

        var allMenus = new Dictionary<string, MenuDefinition>(StringComparer.Ordinal);

        for (var index = 0; index < menus.Count; index++)
        {
            var menu = menus[index];
            var path = $"$.menus[{index}]";
            if (menu is null)
            {
                AddError(issues, "menu.required", "Menu entry is required.", path);
                continue;
            }

            if (!MenuIdentifier.IsTechnicalKey(menu.MenuKey))
            {
                AddError(issues, "menu.key_invalid", "Menu key has an invalid format.", $"{path}.menuKey");
            }
            else if (!allMenus.TryAdd(menu.MenuKey, menu))
            {
                AddError(issues, "menu.key_duplicate", $"Duplicate menu key '{menu.MenuKey}'.", $"{path}.menuKey");
            }

            if (menu.Revision <= 0)
            {
                AddError(issues, "menu.revision_invalid", "Published revision must be positive.", $"{path}.revision");
            }

            if (menu.Status != MenuLifecycleStatus.Published)
            {
                AddError(issues, "menu.not_published", "Runtime Release may contain only published menu revisions.", $"{path}.status");
            }

            if (!MenuScopeMatcher.IsStructurallyValid(menu.Scope))
            {
                AddError(issues, "scope.invalid", "Menu scope is inconsistent.", $"{path}.scope");
            }

            MenuContractValidator.ValidateLocalizedText(menu.Title, required: true, $"{path}.title", issues);
            if (menu.Description is not null)
            {
                MenuContractValidator.ValidateLocalizedText(menu.Description, required: false, $"{path}.description", issues);
            }

            MenuContractValidator.ValidateAccessPolicy(menu.Access, allowInherited: false, $"{path}.access", issues);
            MenuContractValidator.ValidateAudience(menu.Audience, $"{path}.audience", issues);
            ValidateTechnicalOptional(menu.ProviderKey, "menu.provider_key_invalid", $"{path}.providerKey", issues);
            ValidateMetadata(menu.Metadata, $"{path}.metadata", issues);
            if (menu.Items is null)
            {
                AddError(issues, "menu.items_required", "Menu items collection cannot be null.", $"{path}.items");
            }

            if (menu.RequiredFeatures is null)
            {
                AddError(issues, "menu.required_features_required", "Required features collection cannot be null.", $"{path}.requiredFeatures");
            }
        }

        var applicableMenus = allMenus.Values
            .Where(menu => MenuScopeMatcher.IsStructurallyValid(menu.Scope) &&
                           MenuScopeMatcher.AppliesTo(menu.Scope, context.ServerKey, context.ServerGroups))
            .ToDictionary(static menu => menu.MenuKey, StringComparer.Ordinal);

        if (applicableMenus.Count == 0)
        {
            AddWarning(issues, "release.empty_for_server", "Release contains no menus for this server.", "$.menus");
        }

        var dependencyGraph = applicableMenus.Keys.ToDictionary(
            static key => key,
            static _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        var parentGraph = applicableMenus.Keys.ToDictionary(
            static key => key,
            static _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var (menuKey, menu) in applicableMenus)
        {
            var menuIndex = FindMenuIndex(menus, menu);
            var path = $"$.menus[{menuIndex}]";
            ValidateProviderOwner(menu, path, context, issues);
            ValidateMenuReferences(menu, path, applicableMenus, dependencyGraph[menuKey], context, issues);
            if (menu.Parent is { ProviderKey: null } parent
                && applicableMenus.ContainsKey(parent.MenuKey))
            {
                parentGraph[menuKey].Add(parent.MenuKey);
            }

            ValidateDesign(menu, path, applicableMenus, dependencyGraph[menuKey], context, issues);
            ValidateItems(menu, path, applicableMenus, dependencyGraph[menuKey], context, issues);
            ValidateFeatures(menu, path, context, issues);
        }

        ValidateCommands(commands, applicableMenus, context, issues);
        ValidateDependencyGraph(
            dependencyGraph,
            context.MaximumNavigationDepth,
            "dependency",
            "Forward menu dependency",
            issues);
        ValidateDependencyGraph(
            parentGraph,
            context.MaximumNavigationDepth,
            "parent_dependency",
            "Parent menu dependency",
            issues);
        return new MenuReleaseValidationResult(issues);
    }

    private static void ValidateHeader(
        MenuReleaseDefinition release,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        if (release.SchemaVersion != MenuContractVersions.SchemaVersion)
        {
            AddError(
                issues,
                "release.schema_incompatible",
                $"Schema version {release.SchemaVersion} is incompatible with {MenuContractVersions.SchemaVersion}.",
                "$.schemaVersion");
        }

        if (release.MenuCoreApiVersion != MenuContractVersions.MenuCoreApiVersion)
        {
            AddError(
                issues,
                "release.api_incompatible",
                $"Menu API version {release.MenuCoreApiVersion} is incompatible with {MenuContractVersions.MenuCoreApiVersion}.",
                "$.menuCoreApiVersion");
        }

        if (release.ReleaseId <= 0)
        {
            AddError(issues, "release.id_invalid", "Release ID must be positive.", "$.releaseId");
        }

        if (release.GeneratedAt == default)
        {
            AddError(issues, "release.generated_at_invalid", "Release generation time is required.", "$.generatedAt");
        }

        if (!ChecksumRegex().IsMatch(release.Checksum ?? string.Empty))
        {
            AddError(issues, "release.checksum_invalid", "Release checksum must be a hexadecimal SHA-256 value.", "$.checksum");
        }
        else
        {
            var actualChecksum = MenuJson.ComputeChecksum(release);
            if (!MenuJson.FixedTimeChecksumEquals(release.Checksum, actualChecksum))
            {
                AddError(issues, "release.checksum_mismatch", "Release checksum does not match canonical payload.", "$.checksum");
            }
        }

        if (context.Capabilities.SchemaVersion != MenuContractVersions.SchemaVersion ||
            context.Capabilities.MenuCoreApiVersion != MenuContractVersions.MenuCoreApiVersion)
        {
            AddError(issues, "capabilities.incompatible", "Server capability manifest is incompatible with Menu schema/API.", "$");
        }

        if (context.Capabilities.ServerKey is { Length: > 0 } capabilityServer &&
            !string.Equals(capabilityServer, context.ServerKey, StringComparison.Ordinal))
        {
            AddError(issues, "capabilities.server_mismatch", "Capability manifest belongs to another server.", "$");
        }

        ValidateMetadata(release.Metadata, "$.metadata", issues);
    }

    private static void ValidateProviderOwner(
        MenuDefinition menu,
        string path,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        if (menu.ProviderKey is null)
        {
            return;
        }

        ValidateProvider(menu.ProviderKey, null, null, $"{path}.providerKey", context, issues);
    }

    private static void ValidateMenuReferences(
        MenuDefinition menu,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        if (menu.Parent is null)
        {
            return;
        }

        // Parent/Back задают обратную навигацию и намеренно не входят в граф
        // опасных forward-переходов. Сам reference всё равно должен существовать.
        ValidateMenuReference(
            menu.Parent,
            allowProvider: true,
            addDependency: false,
            $"{path}.parent",
            applicableMenus,
            dependencies,
            context,
            issues);
    }

    private static void ValidateDesign(
        MenuDefinition menu,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        var design = menu.Design;
        if (design is null)
        {
            AddError(issues, "design.required", "Menu design is required.", $"{path}.design");
            return;
        }

        if (design.ItemsPerPage is < 1 or > 5)
        {
            AddError(issues, "design.items_per_page_invalid", "Items per page must be between 1 and 5.", $"{path}.design.itemsPerPage");
        }

        if (design.ScrollCooldownMilliseconds is < 0 or > 60_000)
        {
            AddError(issues, "design.scroll_cooldown_invalid", "Scroll cooldown must be between 0 and 60000 ms.", $"{path}.design.scrollCooldownMilliseconds");
        }

        if (design.OverrideColor is not null && !ColorRegex().IsMatch(design.OverrideColor))
        {
            AddError(issues, "design.color_invalid", "Override color has an invalid format.", $"{path}.design.overrideColor");
        }

        if (design.MenuSound is not null &&
            (design.MenuSound.Length > 128 || design.MenuSound.Contains("..", StringComparison.Ordinal) ||
             !AssetKeyRegex().IsMatch(design.MenuSound)))
        {
            AddError(issues, "design.sound_invalid", "Menu sound key/path is unsafe.", $"{path}.design.menuSound");
        }

        ValidateDesignTexts(design.Texts, $"{path}.design.texts", issues);
        ValidateDesignOptions(design.Options, $"{path}.design.options", issues);

        var bindingKeys = new HashSet<string>(StringComparer.Ordinal);
        if (design.KeyBindings is null)
        {
            AddError(issues, "design.key_bindings_required", "Key bindings collection cannot be null.", $"{path}.design.keyBindings");
        }

        var bindings = design.KeyBindings ?? Array.Empty<MenuKeyBindingDefinition>();
        if (bindings.Count > MaximumDesignControls)
        {
            AddError(issues, "design.key_bindings_limit_exceeded", "Too many key bindings.", $"{path}.design.keyBindings");
        }

        for (var index = 0; index < Math.Min(bindings.Count, MaximumDesignControls); index++)
        {
            var binding = bindings[index];
            var bindingPath = $"{path}.design.keyBindings[{index}]";
            if (binding is null)
            {
                AddError(issues, "design.key_binding_required", "Key binding is required.", bindingPath);
                continue;
            }

            ValidateUniqueTechnicalKey(binding.BindingKey, bindingKeys, "design.key_binding", $"{bindingPath}.bindingKey", issues);
            if (!TryParseSwiftlyButton(binding.Button))
            {
                AddError(issues, "design.button_invalid", "Key binding button has an invalid format.", $"{bindingPath}.button");
            }

            var isNavigationBinding = IsNavigationBinding(binding.BindingKey);
            var bindingActionKind = binding.Action?.Kind;
            if (isNavigationBinding && bindingActionKind.HasValue && bindingActionKind != MenuActionKind.None)
            {
                AddError(
                    issues,
                    "design.navigation_binding_action_forbidden",
                    "Navigation key bindings cannot also execute an action.",
                    bindingPath + ".action");
            }
            else if (!isNavigationBinding && bindingActionKind == MenuActionKind.None)
            {
                AddError(
                    issues,
                    "design.custom_binding_action_required",
                    "Custom key bindings require an action.",
                    bindingPath + ".action");
            }

            ValidateAction(binding.Action, bindingPath + ".action", applicableMenus, dependencies, context, issues);
        }

        var extraKeys = new HashSet<string>(StringComparer.Ordinal);
        if (design.ExtraButtons is null)
        {
            AddError(issues, "design.extra_buttons_required", "Extra buttons collection cannot be null.", $"{path}.design.extraButtons");
        }

        var extraButtons = design.ExtraButtons ?? Array.Empty<MenuExtraButtonDefinition>();
        if (extraButtons.Count > MaximumDesignControls)
        {
            AddError(issues, "design.extra_buttons_limit_exceeded", "Too many extra buttons.", $"{path}.design.extraButtons");
        }

        for (var index = 0; index < Math.Min(extraButtons.Count, MaximumDesignControls); index++)
        {
            var button = extraButtons[index];
            var buttonPath = $"{path}.design.extraButtons[{index}]";
            if (button is null)
            {
                AddError(issues, "design.extra_button_required", "Extra button is required.", buttonPath);
                continue;
            }

            ValidateUniqueTechnicalKey(button.ButtonKey, extraKeys, "design.extra_button", $"{buttonPath}.buttonKey", issues);
            MenuContractValidator.ValidateLocalizedText(button.Label, required: true, $"{buttonPath}.label", issues);
            if (!TryParseSwiftlyButton(button.Button))
            {
                AddError(issues, "design.button_invalid", "Extra button key has an invalid format.", $"{buttonPath}.button");
            }

            ValidateAction(button.Action, buttonPath + ".action", applicableMenus, dependencies, context, issues);
        }
    }

    private static void ValidateDesignTexts(
        MenuDesignTextDefinition? texts,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (texts is null)
        {
            AddError(issues, "design.texts_required", "Design texts object is required.", path);
            return;
        }

        var values = new (LocalizedText? Text, string Name)[]
        {
            (texts.AccessTitle, "accessTitle"),
            (texts.MenuTitle, "menuTitle"),
            (texts.NoAccess, "noAccess"),
            (texts.NextPage, "nextPage"),
            (texts.PreviousPage, "previousPage"),
            (texts.CurrentlySelected, "currentlySelected"),
            (texts.CenterMenuText, "centerMenuText"),
            (texts.CenterMenuProperty, "centerMenuProperty"),
            (texts.CenterMenuValue, "centerMenuValue")
        };

        foreach (var (text, name) in values)
        {
            if (text is not null)
            {
                MenuContractValidator.ValidateLocalizedText(text, required: false, $"{path}.{name}", issues);
            }
        }
    }

    private static void ValidateDesignOptions(
        IReadOnlyDictionary<string, JsonElement>? options,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        ValidateMetadata(options, path, issues);
        if (options is null)
        {
            return;
        }

        foreach (var (key, value) in options)
        {
            var optionPath = $"{path}.{key}";
            switch (key)
            {
                case "soundEnabled":
                case "titleVisible":
                case "titleItemCountVisible":
                case "footerVisible":
                case "commentVisible":
                case "autoAdjustVisibleItems":
                    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        AddError(issues, "design.option_type_invalid", "Design option must be boolean.", optionPath);
                    }

                    break;

                case "autoCloseSeconds":
                    if (value.ValueKind != JsonValueKind.Number
                        || !value.TryGetSingle(out var seconds)
                        || !float.IsFinite(seconds)
                        || seconds is < 0f or > 86_400f)
                    {
                        AddError(issues, "design.auto_close_invalid", "Auto-close must be between 0 and 86400 seconds.", optionPath);
                    }

                    break;

                case "scrollStyle":
                    if (value.ValueKind != JsonValueKind.String
                        || !Enum.TryParse<MenuOptionScrollStyle>(value.GetString(), ignoreCase: true, out _))
                    {
                        AddError(issues, "design.scroll_style_invalid", "Scroll style is unsupported by this Swiftly adapter.", optionPath);
                    }

                    break;

                case "navigationMarkerColor":
                case "footerColor":
                case "visualGuideLineColor":
                case "disabledColor":
                    if (value.ValueKind != JsonValueKind.String
                        || !ColorRegex().IsMatch(value.GetString() ?? string.Empty))
                    {
                        AddError(issues, "design.color_invalid", "Design color must use a supported hexadecimal format.", optionPath);
                    }

                    break;

                case "defaultComment":
                    var comment = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                    if (comment is null || comment.Length > 2_048 || comment.Contains('\0'))
                    {
                        AddError(issues, "design.default_comment_invalid", "Default comment is invalid or too long.", optionPath);
                    }

                    break;

                default:
                    AddError(issues, "design.option_unknown", "Design option is not supported by this Swiftly adapter.", optionPath);
                    break;
            }
        }
    }

    private static void ValidateItems(
        MenuDefinition menu,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        var itemKeys = new HashSet<string>(StringComparer.Ordinal);
        var items = menu.Items ?? Array.Empty<MenuItemDefinition>();
        if (items.Count > MaximumItemsPerMenu)
        {
            AddError(issues, "menu.items_limit_exceeded", "Menu contains too many items.", $"{path}.items");
        }

        for (var index = 0; index < Math.Min(items.Count, MaximumItemsPerMenu); index++)
        {
            var item = items[index];
            var itemPath = $"{path}.items[{index}]";
            if (item is null)
            {
                AddError(issues, "item.required", "Menu item is required.", itemPath);
                continue;
            }

            ValidateUniqueTechnicalKey(item.ItemKey, itemKeys, "item.key", $"{itemPath}.itemKey", issues);
            MenuContractValidator.ValidateLocalizedText(item.Text, required: true, $"{itemPath}.text", issues);
            if (item.Comment is not null)
            {
                MenuContractValidator.ValidateLocalizedText(item.Comment, required: false, $"{itemPath}.comment", issues);
            }

            if (item.Style is not null
                && (!StyleRegex().IsMatch(item.Style)
                    || !Enum.TryParse<MenuOptionTextStyle>(item.Style, ignoreCase: true, out _)))
            {
                AddError(issues, "item.style_invalid", "Item style has an invalid format.", $"{itemPath}.style");
            }

            MenuContractValidator.ValidateAccessPolicy(item.Access, allowInherited: true, $"{itemPath}.access", issues);
            if (!Enum.IsDefined(item.NoAccessBehavior))
            {
                AddError(issues, "item.no_access_behavior_invalid", "No-access behavior is unsupported.", $"{itemPath}.noAccessBehavior");
            }

            if (!Enum.IsDefined(item.ProviderUnavailableBehavior))
            {
                AddError(issues, "item.provider_behavior_invalid", "Provider-unavailable behavior is unsupported.", $"{itemPath}.providerUnavailableBehavior");
            }

            ValidateItemValue(item, itemPath, issues);
            ValidateMetadata(item.Metadata, $"{itemPath}.metadata", issues);

            if (item.Action is not null)
            {
                ValidateAction(item.Action, itemPath + ".action", applicableMenus, dependencies, context, issues);
            }

            if (item.OnChange is not null)
            {
                if (item.Kind is not (MenuItemKind.Checkbox or MenuItemKind.Choice or MenuItemKind.Slider))
                {
                    AddError(issues, "item.on_change_invalid", "OnChange is allowed only for Checkbox, Choice or Slider.", $"{itemPath}.onChange");
                }

                ValidateAction(
                    item.OnChange,
                    itemPath + ".onChange",
                    applicableMenus,
                    dependencies,
                    context,
                    issues,
                    InitialOnChangeValue(item));
            }
        }
    }

    private static void ValidateItemValue(
        MenuItemDefinition item,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        var value = item.Value;
        if (value is null)
        {
            AddError(issues, "item.value_required", "Item value object is required.", $"{path}.value");
            return;
        }

        if (value.Initial is { } initialValue)
        {
            ScanForSecrets(initialValue, $"{path}.value.initial", issues);
        }

        if (value.Choices is null)
        {
            AddError(issues, "item.choices_required", "Choices collection cannot be null.", $"{path}.value.choices");
        }

        var choices = value.Choices ?? Array.Empty<MenuChoiceOptionDefinition>();
        switch (item.Kind)
        {
            case MenuItemKind.Text:
            case MenuItemKind.C4:
                RequireNoTypedValue(value, choices, path, issues);
                break;

            case MenuItemKind.Checkbox:
                if (value.Initial is { } checkbox && checkbox.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    AddError(issues, "item.checkbox_initial_invalid", "Checkbox initial value must be boolean.", $"{path}.value.initial");
                }

                if (choices.Count != 0 || value.Minimum is not null || value.Maximum is not null || value.Step is not null)
                {
                    AddError(issues, "item.checkbox_value_invalid", "Checkbox cannot contain choices or slider limits.", $"{path}.value");
                }

                break;

            case MenuItemKind.Choice:
                if (choices.Count > MaximumChoicesPerItem)
                {
                    AddError(issues, "item.choices_limit_exceeded", "Choice contains too many options.", $"{path}.value.choices");
                }

                if (choices.Count == 0)
                {
                    AddError(issues, "item.choice_empty", "Choice must contain at least one option.", $"{path}.value.choices");
                }

                var optionKeys = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < Math.Min(choices.Count, MaximumChoicesPerItem); index++)
                {
                    var choice = choices[index];
                    var choicePath = $"{path}.value.choices[{index}]";
                    if (choice is null)
                    {
                        AddError(issues, "item.choice_required", "Choice option is required.", choicePath);
                        continue;
                    }

                    ValidateUniqueTechnicalKey(choice.OptionKey, optionKeys, "item.choice_key", $"{choicePath}.optionKey", issues);
                    MenuContractValidator.ValidateLocalizedText(choice.Text, required: true, $"{choicePath}.text", issues);
                    ValidateJsonValue(choice.Value, allowNull: true, $"{choicePath}.value", issues);
                    if (choice.Value is { } choiceValue)
                    {
                        ScanForSecrets(choiceValue, $"{choicePath}.value", issues);
                    }
                }

                ValidateChoiceLabels(choices, path, issues);

                if (value.Minimum is not null || value.Maximum is not null || value.Step is not null)
                {
                    AddError(issues, "item.choice_value_invalid", "Choice cannot contain slider limits.", $"{path}.value");
                }

                ValidateJsonValue(value.Initial, allowNull: true, $"{path}.value.initial", issues);
                if (value.Initial is { } choiceInitial && !choices
                        .Take(MaximumChoicesPerItem)
                        .Where(static choice => choice is not null)
                        .Any(choice => JsonEquals(choiceInitial, choice!.Value)))
                {
                    AddError(
                        issues,
                        "item.choice_initial_missing",
                        "Choice initial value must match one of its options.",
                        $"{path}.value.initial");
                }

                break;

            case MenuItemKind.Slider:
                if (choices.Count != 0 || value.Minimum is null || value.Maximum is null || value.Step is null ||
                    value.Minimum > value.Maximum || value.Step <= 0)
                {
                    AddError(issues, "item.slider_limits_invalid", "Slider requires minimum <= maximum and a positive step.", $"{path}.value");
                }

                if (value.Initial is { } sliderInitial)
                {
                    if (sliderInitial.ValueKind != JsonValueKind.Number || !sliderInitial.TryGetDecimal(out var initial) ||
                        value.Minimum is not { } minimum || value.Maximum is not { } maximum ||
                        initial < minimum || initial > maximum ||
                        value.Step is { } step && (step <= 0 || (initial - minimum) % step != 0))
                    {
                        AddError(issues, "item.slider_initial_invalid", "Slider initial value must be a number within its limits.", $"{path}.value.initial");
                    }
                }

                break;

            default:
                AddError(issues, "item.kind_invalid", "Menu item kind is unsupported.", $"{path}.kind");
                break;
        }
    }

    private static void RequireNoTypedValue(
        MenuItemValueDefinition value,
        IReadOnlyCollection<MenuChoiceOptionDefinition> choices,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (value.Initial is not null || choices.Count != 0 || value.Minimum is not null ||
            value.Maximum is not null || value.Step is not null)
        {
            AddError(issues, "item.value_not_allowed", "This item kind does not accept a typed value.", $"{path}.value");
        }
    }

    private static void ValidateChoiceLabels(
        IReadOnlyCollection<MenuChoiceOptionDefinition> choices,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        var validChoices = choices
            .Take(MaximumChoicesPerItem)
            .Where(static choice => choice?.Text is not null)
            .Select(static choice => choice!.Text)
            .ToArray();
        var locales = validChoices
            .SelectMany(static text => text.Translations?.Keys ?? Array.Empty<string>())
            .SelectMany(static locale =>
            {
                var separator = locale.IndexOfAny(['-', '_']);
                return separator > 0 ? new[] { locale, locale[..separator] } : new[] { locale };
            })
            .Append(string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var locale in locales)
        {
            var labels = new HashSet<string>(StringComparer.Ordinal);
            foreach (var text in validChoices)
            {
                var label = ResolveChoiceLabel(text, locale);
                if (label.Length > 128)
                {
                    label = label[..128];
                }

                if (labels.Add(label))
                {
                    continue;
                }

                AddError(
                    issues,
                    "item.choice_label_duplicate",
                    "Choice option labels must remain unique for every locale supported by the menu adapter.",
                    $"{path}.value.choices");
                break;
            }
        }
    }

    private static string ResolveChoiceLabel(LocalizedText text, string locale)
    {
        var translations = text.Translations ?? new Dictionary<string, string>();
        if (locale.Length > 0 && translations.TryGetValue(locale, out var exact))
        {
            return exact ?? string.Empty;
        }

        if (locale.Length > 0)
        {
            var separator = locale.IndexOfAny(['-', '_']);
            var language = separator > 0 ? locale[..separator] : locale;
            foreach (var (key, value) in translations)
            {
                if (key.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    return value ?? string.Empty;
                }
            }
        }

        return text.Default ?? string.Empty;
    }

    private static void ValidateAction(
        MenuActionDefinition? action,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues,
        JsonElement? changedValue = null)
    {
        if (action is null)
        {
            AddError(issues, "action.required", "Action is required.", path);
            return;
        }

        switch (action.Kind)
        {
            case MenuActionKind.None:
                ValidateEmptyAction(action, path, issues);
                break;

            case MenuActionKind.OpenMenu:
                if (action.TargetMenu is null)
                {
                    AddError(issues, "action.target_required", "OpenMenu requires targetMenu.", $"{path}.targetMenu");
                }
                else
                {
                    ValidateMenuReference(
                        action.TargetMenu,
                        allowProvider: false,
                        addDependency: true,
                        $"{path}.targetMenu",
                        applicableMenus,
                        dependencies,
                        context,
                        issues);
                }

                ValidateNoProviderActionFields(action, allowTarget: true, path, issues);
                break;

            case MenuActionKind.OpenProviderMenu:
                if (action.TargetMenu is null)
                {
                    AddError(issues, "action.target_required", "OpenProviderMenu requires targetMenu.", $"{path}.targetMenu");
                }
                else
                {
                    ValidateMenuReference(
                        action.TargetMenu,
                        allowProvider: true,
                        requireProvider: true,
                        addDependency: false,
                        $"{path}.targetMenu",
                        applicableMenus,
                        dependencies,
                        context,
                        issues);
                }

                ValidateNoProviderActionFields(action, allowTarget: true, path, issues);
                break;

            case MenuActionKind.ProviderAction:
                if (action.TargetMenu is not null)
                {
                    AddError(issues, "action.target_not_allowed", "ProviderAction cannot contain targetMenu.", $"{path}.targetMenu");
                }

                if (!MenuIdentifier.IsTechnicalKey(action.ProviderKey))
                {
                    AddError(issues, "action.provider_key_invalid", "ProviderAction requires a valid provider key.", $"{path}.providerKey");
                }

                if (!MenuIdentifier.IsTechnicalKey(action.ProviderActionKey))
                {
                    AddError(issues, "action.key_invalid", "ProviderAction requires a valid action key.", $"{path}.providerActionKey");
                }

                ValidateJsonValue(action.Arguments, allowNull: true, $"{path}.arguments", issues);
                if (action.Arguments is { ValueKind: not JsonValueKind.Object })
                {
                    AddError(
                        issues,
                        "action.arguments_invalid",
                        "Provider action arguments must be a JSON object or null.",
                        $"{path}.arguments");
                }

                if (action.Arguments is { } arguments)
                {
                    ScanForSecrets(arguments, $"{path}.arguments", issues);
                }

                if (MenuIdentifier.IsTechnicalKey(action.ProviderKey) && MenuIdentifier.IsTechnicalKey(action.ProviderActionKey))
                {
                    ValidateProvider(
                        action.ProviderKey!,
                        null,
                        action.ProviderActionKey,
                        path,
                        context,
                        issues,
                        MenuActionArguments.Compose(action.Arguments, changedValue));
                }

                break;

            case MenuActionKind.Back:
            case MenuActionKind.Close:
                ValidateEmptyAction(action, path, issues);
                break;

            default:
                AddError(issues, "action.kind_invalid", "Action kind is unsupported.", $"{path}.kind");
                break;
        }

        if (!Enum.IsDefined(action.ProviderUnavailableBehavior))
        {
            AddError(
                issues,
                "action.provider_behavior_invalid",
                "Provider-unavailable behavior is unsupported.",
                $"{path}.providerUnavailableBehavior");
        }
    }

    private static JsonElement? InitialOnChangeValue(MenuItemDefinition item)
    {
        var value = item.Value;
        if (value is null)
        {
            return null;
        }

        return item.Kind switch
        {
            MenuItemKind.Checkbox => value.Initial is
                { ValueKind: JsonValueKind.True or JsonValueKind.False } checkbox
                    ? checkbox.Clone()
                    : JsonSerializer.SerializeToElement(false),
            MenuItemKind.Choice => value.Initial?.Clone()
                                   ?? value.Choices?.FirstOrDefault()?.Value?.Clone()
                                   ?? JsonSerializer.SerializeToElement<object?>(null),
            MenuItemKind.Slider => value.Initial?.Clone()
                                   ?? JsonSerializer.SerializeToElement(value.Minimum ?? 0m),
            _ => null,
        };
    }

    private static void ValidateMenuReference(
        MenuReferenceDefinition reference,
        bool allowProvider,
        bool addDependency,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        ValidateMenuReference(reference, allowProvider, requireProvider: false, addDependency, path, applicableMenus, dependencies, context, issues);
    }

    private static void ValidateMenuReference(
        MenuReferenceDefinition reference,
        bool allowProvider,
        bool requireProvider,
        bool addDependency,
        string path,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        ISet<string> dependencies,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        if (!MenuIdentifier.IsTechnicalKey(reference.MenuKey))
        {
            AddError(issues, "reference.menu_key_invalid", "Referenced menu key has an invalid format.", $"{path}.menuKey");
            return;
        }

        if (reference.ProviderKey is null)
        {
            if (requireProvider)
            {
                AddError(issues, "reference.provider_required", "Provider menu reference requires providerKey.", $"{path}.providerKey");
                return;
            }

            if (!applicableMenus.ContainsKey(reference.MenuKey))
            {
                AddError(issues, "reference.menu_missing", $"Referenced menu '{reference.MenuKey}' is not available for this server.", path);
            }
            else if (addDependency)
            {
                dependencies.Add(reference.MenuKey);
            }

            return;
        }

        if (!allowProvider)
        {
            AddError(issues, "reference.provider_not_allowed", "This reference must target a menu in the active Release.", $"{path}.providerKey");
            return;
        }

        if (!MenuIdentifier.IsTechnicalKey(reference.ProviderKey))
        {
            AddError(issues, "reference.provider_key_invalid", "Referenced provider key has an invalid format.", $"{path}.providerKey");
            return;
        }

        ValidateProvider(reference.ProviderKey, reference.MenuKey, null, path, context, issues);
    }

    private static void ValidateProvider(
        string providerKey,
        string? menuKey,
        string? actionKey,
        string path,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues,
        JsonElement? arguments = null)
    {
        if (!context.Providers.TryGet(providerKey, out var provider))
        {
            AddError(issues, "provider.missing", $"Provider '{providerKey}' is not registered for this server.", path);
            return;
        }

        if (provider.MenuApiVersion != MenuContractVersions.MenuCoreApiVersion ||
            provider.Availability == ProviderAvailability.Incompatible)
        {
            AddError(issues, "provider.incompatible", $"Provider '{providerKey}' uses an incompatible Menu API.", path);
            return;
        }

        if (provider.Availability == ProviderAvailability.Error)
        {
            AddError(issues, "provider.error", $"Provider '{providerKey}' is in an error state.", path);
        }
        else if (provider.Availability == ProviderAvailability.Offline)
        {
            AddWarning(issues, "provider.offline", $"Provider '{providerKey}' is currently offline.", path);
        }

        if (menuKey is not null && !provider.MenuKeys.Contains(menuKey))
        {
            AddError(issues, "provider.menu_missing", $"Provider '{providerKey}' does not export menu '{menuKey}'.", path);
        }

        if (actionKey is null)
        {
            return;
        }

        if (!provider.ActionKeys.Contains(actionKey))
        {
            AddError(issues, "provider.action_missing", $"Provider '{providerKey}' does not export action '{actionKey}'.", path);
            return;
        }

        if (provider.ArgumentSchemas.TryGetValue(actionKey, out var schema))
        {
            AddProviderIssues(
                ProviderJsonSchemaValidator.Validate(arguments, schema),
                providerKey,
                path,
                issues);
        }

        if (!provider.ArgumentValidators.TryGetValue(actionKey, out var validator))
        {
            return;
        }

        try
        {
            AddProviderIssues(validator(arguments), providerKey, path, issues);
        }
        catch (Exception exception)
        {
            AddError(
                issues,
                "provider.validator_failed",
                $"Provider '{providerKey}' argument validator failed: {exception.GetType().Name}.",
                path + ".arguments");
        }
    }

    private static void AddProviderIssues(
        MenuValidationResult? result,
        string providerKey,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (result is null)
        {
            AddError(
                issues,
                "provider.validator_result_invalid",
                $"Provider '{providerKey}' returned no validation result.",
                path + ".arguments");
            return;
        }

        var providerIssues = result.Issues ?? Array.Empty<MenuValidationIssue>();
        if (providerIssues.Count > 64)
        {
            AddError(
                issues,
                "provider.validator_issue_limit_exceeded",
                $"Provider '{providerKey}' returned too many validation issues.",
                path + ".arguments");
        }

        foreach (var providerIssue in providerIssues.Take(64))
        {
            if (providerIssue is null)
            {
                continue;
            }

            var relativePath = providerIssue.Path?.TrimStart('$', '.');
            issues.Add(providerIssue with
            {
                Code = string.IsNullOrWhiteSpace(providerIssue.Code)
                    ? "provider.arguments_invalid"
                    : providerIssue.Code,
                Message = string.IsNullOrWhiteSpace(providerIssue.Message)
                    ? $"Provider '{providerKey}' rejected action arguments."
                    : providerIssue.Message,
                Path = string.IsNullOrEmpty(relativePath)
                    ? path + ".arguments"
                    : $"{path}.arguments.{relativePath}"
            });
        }

        if (!result.IsValid && !providerIssues.Any(static issue => issue?.Severity == MenuValidationSeverity.Error))
        {
            AddError(
                issues,
                "provider.arguments_invalid",
                $"Provider '{providerKey}' rejected action arguments.",
                path + ".arguments");
        }
    }

    private static void ValidateFeatures(
        MenuDefinition menu,
        string path,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        var features = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in menu.RequiredFeatures ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(feature) || !FeatureKeyRegex().IsMatch(feature))
            {
                AddError(issues, "feature.key_invalid", "Required feature key has an invalid format.", $"{path}.requiredFeatures");
                continue;
            }

            if (!features.Add(feature))
            {
                AddError(issues, "feature.duplicate", $"Required feature '{feature}' is duplicated.", $"{path}.requiredFeatures");
            }
        }

        foreach (var item in (menu.Items ?? Array.Empty<MenuItemDefinition>()).Take(MaximumItemsPerMenu))
        {
            if (item is null)
            {
                continue;
            }

            var inferred = item.Kind switch
            {
                MenuItemKind.Checkbox => MenuFeatureKeys.Checkbox,
                MenuItemKind.Choice => MenuFeatureKeys.Choice,
                MenuItemKind.Slider => MenuFeatureKeys.Slider,
                MenuItemKind.C4 => MenuFeatureKeys.C4,
                _ => null
            };

            if (inferred is not null)
            {
                features.Add(inferred);
            }
        }

        if (menu.Parent is not null && menu.Design?.ParentNavigation == true)
        {
            features.Add(MenuFeatureKeys.ParentNavigation);
        }

        if ((menu.Design?.KeyBindings?.Count ?? 0) != 0)
        {
            features.Add(MenuFeatureKeys.CustomKeyBinds);
        }

        if ((menu.Design?.ExtraButtons?.Count ?? 0) != 0)
        {
            features.Add(MenuFeatureKeys.ExtraButtons);
        }

        if (menu.Design?.WelcomeScreen == true)
        {
            features.Add(MenuFeatureKeys.WelcomeScreen);
        }

        if (menu.Design?.OverlayOnly == true)
        {
            features.Add(MenuFeatureKeys.OverlayOnly);
        }

        if (menu.Design?.WrapNavigation == true)
        {
            features.Add(MenuFeatureKeys.WrapNavigation);
        }

        if (menu.Design?.ScrollCooldownMilliseconds is not null)
        {
            features.Add(MenuFeatureKeys.ScrollCooldown);
        }

        if (menu.Design?.OverrideColor is not null)
        {
            features.Add(MenuFeatureKeys.OverrideColor);
        }

        if (menu.Design?.MenuSound is not null)
        {
            features.Add(MenuFeatureKeys.MenuSound);
        }

        if (menu.Design?.Texts?.AccessTitle is not null)
        {
            features.Add(MenuFeatureKeys.AccessTitle);
        }

        if (menu.Design?.Texts?.NextPage is not null)
        {
            features.Add(MenuFeatureKeys.NextPageText);
        }

        if (menu.Design?.Texts?.PreviousPage is not null)
        {
            features.Add(MenuFeatureKeys.PreviousPageText);
        }

        if (menu.Design?.Texts?.CurrentlySelected is not null)
        {
            features.Add(MenuFeatureKeys.CurrentlySelectedText);
        }

        if (menu.Design?.Texts is { } texts
            && (texts.CenterMenuText is not null
                || texts.CenterMenuProperty is not null
                || texts.CenterMenuValue is not null))
        {
            features.Add(MenuFeatureKeys.CenterMenuText);
        }

        if (menu.Design?.Options is { } options)
        {
            if (options.ContainsKey("soundEnabled"))
            {
                features.Add(MenuFeatureKeys.SoundToggle);
            }

            if (options.ContainsKey("autoCloseSeconds"))
            {
                features.Add(MenuFeatureKeys.AutoClose);
            }
        }

        foreach (var feature in features)
        {
            if (!context.Capabilities.Supports(feature))
            {
                AddError(
                    issues,
                    "feature.unsupported",
                    $"Target server does not support feature '{feature}'.",
                    $"{path}.requiredFeatures");
            }
        }
    }

    private static void ValidateCommands(
        IReadOnlyList<MenuCommandDefinition> commands,
        IReadOnlyDictionary<string, MenuDefinition> applicableMenus,
        MenuReleaseValidationContext context,
        ICollection<MenuValidationIssue> issues)
    {
        var commandKeys = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < commands.Count; index++)
        {
            var command = commands[index];
            var path = $"$.commands[{index}]";
            if (command is null)
            {
                AddError(issues, "command.required", "Command entry is required.", path);
                continue;
            }

            ValidateUniqueTechnicalKey(command.CommandKey, commandKeys, "command.key", $"{path}.commandKey", issues);
            if (!Enum.IsDefined(command.Kind))
            {
                AddError(issues, "command.kind_invalid", "Command kind is unsupported.", $"{path}.kind");
            }

            if (!Enum.IsDefined(command.ChatSuppression))
            {
                AddError(issues, "command.suppression_invalid", "Chat suppression mode is unsupported.", $"{path}.chatSuppression");
            }

            if (!MenuIdentifier.IsAliasValid(command.Kind, command.Alias))
            {
                AddError(issues, "command.alias_invalid", "Command alias has an invalid format.", $"{path}.alias");
            }
            else if (!string.Equals(command.Alias, MenuIdentifier.CanonicalizeAlias(command.Alias), StringComparison.Ordinal))
            {
                AddWarning(issues, "command.alias_normalized", "Command alias will be normalized to Unicode NFC and trimmed.", $"{path}.alias");
            }

            if (!MenuIdentifier.IsTechnicalKey(command.MenuKey))
            {
                AddError(issues, "command.menu_key_invalid", "Command target menu key has an invalid format.", $"{path}.menuKey");
            }

            if (!MenuScopeMatcher.IsStructurallyValid(command.Scope))
            {
                AddError(issues, "scope.invalid", "Command scope is inconsistent.", $"{path}.scope");
                continue;
            }

            if (command.Kind == MenuCommandKind.Console && command.ChatSuppression != ChatSuppressionMode.None)
            {
                AddError(issues, "command.suppression_invalid", "Chat suppression is valid only for chat aliases.", $"{path}.chatSuppression");
            }

            if (!command.Enabled || !MenuScopeMatcher.AppliesTo(command.Scope, context.ServerKey, context.ServerGroups))
            {
                continue;
            }

            if (!applicableMenus.ContainsKey(command.MenuKey))
            {
                AddError(issues, "command.menu_missing", $"Command target menu '{command.MenuKey}' is not available for this server.", $"{path}.menuKey");
            }

            if (!MenuIdentifier.IsAliasValid(command.Kind, command.Alias))
            {
                continue;
            }

            var lookupKey = MenuIdentifier.CommandLookupKey(command.Kind, command.Alias);
            if (context.ReservedCommandLookupKeys.Contains(lookupKey))
            {
                AddError(issues, "command.reserved", $"Command alias '{command.Alias}' is reserved by Menu.Core.", $"{path}.alias");
            }

            if (aliases.TryGetValue(lookupKey, out var existingMenu))
            {
                AddError(
                    issues,
                    "command.alias_duplicate",
                    $"Command alias '{command.Alias}' conflicts with target menu '{existingMenu}'.",
                    $"{path}.alias");
            }
            else
            {
                aliases.Add(lookupKey, command.MenuKey);
            }
        }
    }

    private static void ValidateDependencyGraph(
        IReadOnlyDictionary<string, HashSet<string>> graph,
        int maximumDepth,
        string codePrefix,
        string description,
        ICollection<MenuValidationIssue> issues)
    {
        var indegrees = graph.Keys.ToDictionary(static node => node, static _ => 0, StringComparer.Ordinal);
        foreach (var targets in graph.Values)
        {
            foreach (var target in targets)
            {
                indegrees[target]++;
            }
        }

        var queue = new Queue<string>(indegrees.Where(static pair => pair.Value == 0).Select(static pair => pair.Key));
        var depths = graph.Keys.ToDictionary(static node => node, static _ => 0, StringComparer.Ordinal);
        var processed = 0;
        var deepestNode = string.Empty;
        var deepest = 0;

        while (queue.TryDequeue(out var node))
        {
            processed++;
            if (depths[node] > deepest)
            {
                deepest = depths[node];
                deepestNode = node;
            }

            foreach (var target in graph[node])
            {
                depths[target] = Math.Max(depths[target], depths[node] + 1);
                if (--indegrees[target] == 0)
                {
                    queue.Enqueue(target);
                }
            }
        }

        if (processed != graph.Count)
        {
            var cycleNodes = string.Join(", ", indegrees
                .Where(static pair => pair.Value > 0)
                .Select(static pair => pair.Key)
                .Take(16));
            AddError(
                issues,
                codePrefix + ".cycle",
                $"{description} cycle detected near: {cycleNodes}.",
                "$.menus");
        }

        if (deepest > maximumDepth)
        {
            AddError(
                issues,
                codePrefix + ".depth_exceeded",
                $"{description} depth to '{deepestNode}' is {deepest}, exceeding runtime limit {maximumDepth}.",
                "$.menus");
        }
    }

    private static void ValidateEmptyAction(
        MenuActionDefinition action,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (action.TargetMenu is not null || action.ProviderKey is not null || action.ProviderActionKey is not null ||
            action.Arguments is not null)
        {
            AddError(issues, "action.fields_not_allowed", "This action kind cannot contain target, provider or arguments.", path);
        }
    }

    private static void ValidateNoProviderActionFields(
        MenuActionDefinition action,
        bool allowTarget,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if ((!allowTarget && action.TargetMenu is not null) || action.ProviderKey is not null ||
            action.ProviderActionKey is not null || action.Arguments is not null)
        {
            AddError(issues, "action.fields_not_allowed", "Action contains fields that do not belong to its kind.", path);
        }
    }

    private static void ValidateUniqueTechnicalKey(
        string? value,
        ISet<string> values,
        string codePrefix,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (!MenuIdentifier.IsTechnicalKey(value))
        {
            AddError(issues, codePrefix + "_invalid", "Technical key has an invalid format.", path);
        }
        else if (!values.Add(value!))
        {
            AddError(issues, codePrefix + "_duplicate", $"Duplicate technical key '{value}'.", path);
        }
    }

    private static bool IsNavigationBinding(string? bindingKey)
        => bindingKey is "select" or "forward" or "backward" or "exit";

    private static void ValidateTechnicalOptional(
        string? value,
        string code,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (value is not null && !MenuIdentifier.IsTechnicalKey(value))
        {
            AddError(issues, code, "Technical key has an invalid format.", path);
        }
    }

    private static bool TryParseSwiftlyButton(string? value) =>
        SwiftlyButtonParser.TryParse(value, out _);

    private static void ValidateMetadata(
        IReadOnlyDictionary<string, JsonElement>? metadata,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (metadata is null)
        {
            AddError(issues, "metadata.required", "Metadata object cannot be null.", path);
            return;
        }

        if (metadata.Count > MaximumMetadataEntries)
        {
            AddError(issues, "metadata.limit_exceeded", "Metadata contains too many entries.", path);
            return;
        }

        foreach (var (key, value) in metadata)
        {
            if (!JsonObjectKeyRegex().IsMatch(key))
            {
                AddError(issues, "metadata.key_invalid", "Metadata key has an invalid format.", path);
            }

            ValidateJsonValue(value, allowNull: true, $"{path}.{key}", issues);
            ScanForSecrets(value, $"{path}.{key}", issues);
        }
    }

    private static void ValidateJsonValue(
        JsonElement? value,
        bool allowNull,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (value is null)
        {
            if (!allowNull)
            {
                AddError(issues, "json.required", "JSON value is required.", path);
            }

            return;
        }

        if (value.Value.ValueKind == JsonValueKind.Undefined || (!allowNull && value.Value.ValueKind == JsonValueKind.Null))
        {
            AddError(issues, "json.invalid", "JSON value is undefined or null.", path);
        }
    }

    private static bool JsonEquals(JsonElement left, JsonElement? right)
    {
        if (left.ValueKind == JsonValueKind.Undefined || right?.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        if (right is null)
        {
            return left.ValueKind == JsonValueKind.Null;
        }

        try
        {
            return MenuJson.Canonicalize(left).AsSpan().SequenceEqual(MenuJson.Canonicalize(right.Value));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ScanForSecrets(
        JsonElement value,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (!JsonObjectKeyRegex().IsMatch(property.Name))
                {
                    AddError(
                        issues,
                        "payload.key_invalid",
                        "Arbitrary JSON object keys must use the portable ASCII format.",
                        $"{path}.{property.Name}");
                }

                if (SensitiveKeyRegex().IsMatch(property.Name))
                {
                    AddError(
                        issues,
                        "payload.secret_forbidden",
                        $"Potential credential field '{property.Name}' is forbidden in menu payload.",
                        $"{path}.{property.Name}");
                }

                ScanForSecrets(property.Value, $"{path}.{property.Name}", issues);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                ScanForSecrets(item, $"{path}[{index++}]", issues);
            }
        }
    }

    private static int FindMenuIndex(IReadOnlyList<MenuDefinition> menus, MenuDefinition menu)
    {
        for (var index = 0; index < menus.Count; index++)
        {
            if (ReferenceEquals(menus[index], menu))
            {
                return index;
            }
        }

        return 0;
    }

    private static void AddError(
        ICollection<MenuValidationIssue> issues,
        string code,
        string message,
        string? path)
    {
        issues.Add(new MenuValidationIssue
        {
            Severity = MenuValidationSeverity.Error,
            Code = code,
            Message = message,
            Path = path
        });
    }

    private static void AddWarning(
        ICollection<MenuValidationIssue> issues,
        string code,
        string message,
        string? path)
    {
        issues.Add(new MenuValidationIssue
        {
            Severity = MenuValidationSeverity.Warning,
            Code = code,
            Message = message,
            Path = path
        });
    }

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChecksumRegex();

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();

    [GeneratedRegex("^[a-zA-Z0-9_./-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex AssetKeyRegex();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex FeatureKeyRegex();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex StyleRegex();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9._-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex JsonObjectKeyRegex();

    [GeneratedRegex("(?:password|passwd|secret|token|credential|connection.?string|api.?key|private.?key)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveKeyRegex();
}
