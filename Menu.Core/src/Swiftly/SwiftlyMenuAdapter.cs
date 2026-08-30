using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Results;
using Menu.Core.Access;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;

namespace Menu.Core.Swiftly;

/// <summary>
/// Единственное место, преобразующее нормализованную схему v1 в Swiftly beta.8.
/// </summary>
internal sealed class SwiftlyMenuAdapter(
    ISwiftlyCore core,
    AdminAccessResolver accessResolver,
    ILogger<SwiftlyMenuAdapter> logger)
{
    internal MenuOperationResult Open(MenuRenderRequest request)
    {
        try
        {
            var menu = Build(request);
            core.MenusAPI.OpenMenuForPlayer(request.Target, menu);
            return MenuOperationResult.Succeeded;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось построить menu {MenuKey} для игрока {PlayerId}.",
                request.Menu.MenuKey, request.Target.PlayerID);
            return MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "swiftly_menu_build_failed");
        }
    }

    internal IMenuAPI Build(MenuRenderRequest request)
    {
        var definition = request.Menu;
        var design = definition.Design;
        var builder = core.MenusAPI.CreateBuilder();

        if (design.CanClose) builder.EnableExit();
        else builder.DisableExit();

        builder.SetPlayerFrozen(design.FreezePlayer);
        if (GetBoolean(design.Options, "soundEnabled", true)) builder.EnableSound();
        else builder.DisableSound();

        if (GetSingle(design.Options, "autoCloseSeconds") is { } autoCloseSeconds
            && autoCloseSeconds is >= 0f and <= 86_400f)
        {
            builder.SetAutoCloseDelay(autoCloseSeconds);
        }

        ConfigureDesign(builder, definition, request.Locale);

        if (design.ParentNavigation
            && definition.Parent is not null
            && request.BuildParent is not null)
        {
            var parent = request.BuildParent(definition.Parent, request.Depth + 1);
            if (parent is not null)
            {
                builder.BindToParent(parent);
            }
        }

        foreach (var item in definition.Items)
        {
            var option = BuildOption(request, item);
            if (option is not null)
            {
                builder.AddOption(option);
            }
        }

        ConfigureButtons(builder, request);
        return builder.Build();
    }

    private void ConfigureDesign(
        IMenuBuilderAPI builder,
        MenuDefinition menu,
        string locale)
    {
        var design = menu.Design;
        var title = design.Texts.MenuTitle ?? menu.Title;
        builder.Design.SetMenuTitle(MenuTextRenderer.Render(title, locale, 128));

        builder.Design.SetMenuTitleVisible(GetBoolean(design.Options, "titleVisible", true));
        builder.Design.SetMenuTitleItemCountVisible(GetBoolean(design.Options, "titleItemCountVisible", true));
        builder.Design.SetMenuFooterVisible(GetBoolean(design.Options, "footerVisible", true));
        builder.Design.SetCommentVisible(GetBoolean(design.Options, "commentVisible", true));

        if (design.ItemsPerPage is >= 1 and <= 5)
        {
            builder.Design.SetMaxVisibleItems(design.ItemsPerPage.Value);
        }

        if (GetBoolean(design.Options, "autoAdjustVisibleItems", true))
        {
            builder.Design.EnableAutoAdjustVisibleItems();
        }
        else
        {
            builder.Design.DisableAutoAdjustVisibleItems();
        }

        if (GetString(design.Options, "scrollStyle") is { } scrollStyle
            && Enum.TryParse<MenuOptionScrollStyle>(scrollStyle, ignoreCase: true, out var parsedScroll))
        {
            builder.Design.SetGlobalScrollStyle(parsedScroll);
        }

        ApplyColor(design.Options, "navigationMarkerColor", builder.Design.SetNavigationMarkerColor);
        ApplyColor(design.Options, "footerColor", builder.Design.SetMenuFooterColor);
        ApplyColor(design.Options, "visualGuideLineColor", builder.Design.SetVisualGuideLineColor);
        ApplyColor(design.Options, "disabledColor", builder.Design.SetDisabledColor);

        if (design.OverrideColor is { } overrideColor)
        {
            builder.Design.SetNavigationMarkerColor(overrideColor);
            builder.Design.SetMenuFooterColor(overrideColor);
            builder.Design.SetVisualGuideLineColor(overrideColor);
            builder.Design.SetDisabledColor(overrideColor);
        }

        if (GetString(design.Options, "defaultComment") is { } defaultComment)
        {
            builder.Design.SetDefaultComment(MenuTextRenderer.Render(
                new LocalizedText { Default = defaultComment }, null, 128));
        }
    }

    private MenuOptionBase? BuildOption(MenuRenderRequest request, MenuItemDefinition item)
    {
        if (item.Disabled && !request.Menu.Design.ShowDisabledItems)
        {
            return null;
        }

        var hasAccess = accessResolver.CanAccess(request.Target, item.Access, request.Menu.Access);
        if (!hasAccess && item.NoAccessBehavior == MenuNoAccessBehavior.Hide)
        {
            return null;
        }

        if (!hasAccess && item.NoAccessBehavior == MenuNoAccessBehavior.ShowNoAccess)
        {
            var denied = new ButtonMenuOption(MenuTextRenderer.Render(item.Text, request.Locale, 256));
            denied.Comment = MenuTextRenderer.Render(item.Comment, request.Locale, 256);
            ApplyStyle(denied, item.Style);
            denied.Click += (_, _) =>
            {
                core.Scheduler.NextTick(() => request.Target.SendChat(NoAccessText(request)));
                return ValueTask.CompletedTask;
            };
            return denied;
        }

        var actionAvailable = item.Action is null || request.IsActionAvailable(item.Action);
        var changeAvailable = item.OnChange is null || request.IsActionAvailable(item.OnChange);
        var providerAvailable = actionAvailable && changeAvailable;
        if (!providerAvailable && ShouldHideUnavailableAction(item, actionAvailable, changeAvailable))
        {
            return null;
        }

        var text = MenuTextRenderer.Render(item.Text, request.Locale, 256);
        var enabled = !item.Disabled && providerAvailable;
        MenuOptionBase option = item.Kind switch
        {
            MenuItemKind.Checkbox => BuildCheckbox(request, item, text),
            MenuItemKind.Choice => BuildChoice(request, item, text),
            MenuItemKind.Slider => BuildSlider(request, item, text),
            MenuItemKind.Text when item.Action is not null => BuildButton(request, item, text),
            MenuItemKind.Text => new TextMenuOption(text),
            _ => new TextMenuOption(text),
        };

        option.Comment = MenuTextRenderer.Render(item.Comment, request.Locale, 256);
        option.Enabled = enabled;
        ApplyStyle(option, item.Style);

        if (!hasAccess)
        {
            option.Enabled = false;
        }

        return option;
    }

    private static bool ShouldHideUnavailableAction(
        MenuItemDefinition item,
        bool actionAvailable,
        bool changeAvailable)
    {
        return item.ProviderUnavailableBehavior == ProviderUnavailableBehavior.Hide
               || !actionAvailable
               && item.Action?.ProviderUnavailableBehavior == ProviderUnavailableBehavior.Hide
               || !changeAvailable
               && item.OnChange?.ProviderUnavailableBehavior == ProviderUnavailableBehavior.Hide;
    }

    private ButtonMenuOption BuildButton(
        MenuRenderRequest request,
        MenuItemDefinition item,
        string text)
    {
        var option = new ButtonMenuOption(text)
        {
            CloseAfterClick = item.Action?.Kind == MenuActionKind.Close,
        };
        if (item.Action is { } action)
        {
            option.Click += (_, _) => ScheduleAction(request, item, action, null);
        }

        return option;
    }

    private ToggleMenuOption BuildCheckbox(
        MenuRenderRequest request,
        MenuItemDefinition item,
        string text)
    {
        var initial = item.Value.Initial is { ValueKind: JsonValueKind.True };
        var option = new ToggleMenuOption(text, initial);
        if (item.OnChange is { } action)
        {
            option.ValueChanged += (_, args) =>
                ScheduleActionOnGameThread(request, item, action, JsonSerializer.SerializeToElement(args.NewValue));
        }

        if (item.Action is { } clickAction)
        {
            option.Click += (_, _) => ScheduleAction(request, item, clickAction, null);
        }

        return option;
    }

    private ChoiceMenuOption BuildChoice(
        MenuRenderRequest request,
        MenuItemDefinition item,
        string text)
    {
        var labels = item.Value.Choices
            .Select(choice => MenuTextRenderer.Render(choice.Text, request.Locale, 128))
            .ToArray();
        var initialIndex = ResolveInitialChoiceIndex(item.Value);
        var defaultChoice = labels.Length == 0 ? null : labels[Math.Clamp(initialIndex, 0, labels.Length - 1)];
        var option = new ChoiceMenuOption(text, labels, defaultChoice);

        if (item.OnChange is { } action)
        {
            option.ValueChanged += (_, args) =>
            {
                var index = Array.IndexOf(labels, args.NewValue);
                var value = index >= 0 && index < item.Value.Choices.Count
                    ? item.Value.Choices[index].Value
                      ?? JsonSerializer.SerializeToElement<object?>(null)
                    : JsonSerializer.SerializeToElement(args.NewValue);
                ScheduleActionOnGameThread(request, item, action, value);
            };
        }

        if (item.Action is { } clickAction)
        {
            option.Click += (_, _) => ScheduleAction(request, item, clickAction, null);
        }

        return option;
    }

    private SliderMenuOption BuildSlider(
        MenuRenderRequest request,
        MenuItemDefinition item,
        string text)
    {
        var minimum = (float)(item.Value.Minimum ?? 0m);
        var maximum = (float)(item.Value.Maximum ?? 100m);
        var step = (float)(item.Value.Step ?? 1m);
        var initial = item.Value.Initial is { } value && value.TryGetSingle(out var parsed)
            ? parsed
            : minimum;
        var option = new SliderMenuOption(text, minimum, maximum, initial, step);

        if (item.OnChange is { } action)
        {
            option.ValueChanged += (_, args) =>
                ScheduleActionOnGameThread(request, item, action, JsonSerializer.SerializeToElement(args.NewValue));
        }

        if (item.Action is { } clickAction)
        {
            option.Click += (_, _) => ScheduleAction(request, item, clickAction, null);
        }

        return option;
    }

    private void ConfigureButtons(IMenuBuilderAPI builder, MenuRenderRequest request)
    {
        foreach (var binding in request.Menu.Design.KeyBindings)
        {
            if (!TryParseButton(binding.Button, out var button))
            {
                continue;
            }

            switch (binding.BindingKey)
            {
                case "select": builder.SetSelectButton(button); break;
                case "forward": builder.SetMoveForwardButton(button); break;
                case "backward": builder.SetMoveBackwardButton(button); break;
                case "exit": builder.SetExitButton(button); break;
                default:
                    builder.AddExtraButton(button, binding.BindingKey, (_, _) =>
                        ScheduleActionOnGameThread(request, null, binding.Action, null));
                    continue;
            }
        }

        foreach (var extra in request.Menu.Design.ExtraButtons)
        {
            if (!TryParseButton(extra.Button, out var button))
            {
                continue;
            }

            var label = MenuTextRenderer.Render(extra.Label, request.Locale, 64);
            builder.AddExtraButton(button, label, (_, _) =>
                ScheduleActionOnGameThread(request, null, extra.Action, null));
        }
    }

    private ValueTask ScheduleAction(
        MenuRenderRequest request,
        MenuItemDefinition item,
        MenuActionDefinition action,
        JsonElement? value)
    {
        ScheduleActionOnGameThread(request, item, action, value);
        return ValueTask.CompletedTask;
    }

    private void ScheduleActionOnGameThread(
        MenuRenderRequest request,
        MenuItemDefinition? item,
        MenuActionDefinition action,
        JsonElement? value)
    {
        core.Scheduler.NextTick(() => _ = request.Execute(item, action, value));
    }

    private static void ApplyStyle(MenuOptionBase option, string? style)
    {
        if (!string.IsNullOrWhiteSpace(style)
            && Enum.TryParse<MenuOptionTextStyle>(style, ignoreCase: true, out var parsed))
        {
            option.TextStyle = parsed;
        }
    }

    private static int ResolveInitialChoiceIndex(MenuItemValueDefinition value)
    {
        if (value.Initial is not { } initial)
        {
            return 0;
        }

        for (var index = 0; index < value.Choices.Count; index++)
        {
            if (value.Choices[index].Value is { } candidate
                && JsonElement.DeepEquals(candidate, initial))
            {
                return index;
            }
        }

        return 0;
    }

    private static string NoAccessText(MenuRenderRequest request)
    {
        var text = request.Menu.Design.Texts.NoAccess;
        return string.IsNullOrWhiteSpace(text?.Default)
            ? "[Menu] Нет доступа."
            : MenuTextRenderer.Render(text, request.Locale, 256);
    }

    private static bool TryParseButton(string? value, out KeyBind button) =>
        SwiftlyButtonParser.TryParse(value, out button);

    private static bool GetBoolean(
        IReadOnlyDictionary<string, JsonElement> options,
        string key,
        bool fallback) =>
        options.TryGetValue(key, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;

    private static string? GetString(
        IReadOnlyDictionary<string, JsonElement> options,
        string key) =>
        options.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static float? GetSingle(
        IReadOnlyDictionary<string, JsonElement> options,
        string key) =>
        options.TryGetValue(key, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetSingle(out var number)
            ? number
            : null;

    private static void ApplyColor(
        IReadOnlyDictionary<string, JsonElement> options,
        string key,
        Func<string?, IMenuBuilderAPI> setter)
    {
        if (GetString(options, key) is { } color)
        {
            setter(color);
        }
    }
}
