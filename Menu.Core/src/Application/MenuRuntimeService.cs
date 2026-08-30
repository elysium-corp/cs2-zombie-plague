using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Providers;
using Menu.Api.Results;
using Menu.Core.Access;
using Menu.Core.Audience;
using Menu.Core.Configuration;
using Menu.Core.Commands;
using Menu.Core.Providers;
using Menu.Core.Runtime;
using Menu.Core.Swiftly;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Application;

/// <summary>
/// Выполняет hot-path операций меню только над одним immutable snapshot.
/// </summary>
internal sealed class MenuRuntimeService(
    ISwiftlyCore core,
    MenuSnapshotStore snapshots,
    AdminAccessResolver accessResolver,
    MenuAudienceResolver audienceResolver,
    ProviderRegistry providers,
    SwiftlyMenuAdapter adapter,
    MenuCapabilityProvider capabilityProvider,
    IOptions<MenuCoreConfig> options) : IMenuCommandTarget
{
    private static readonly JsonElement EmptyArguments = JsonSerializer.SerializeToElement(new { });
    private readonly MenuCoreConfig _configuration = options.Value;

    internal MenuCapabilityManifest Capabilities => capabilityProvider.Current;

    internal MenuOperationResult OpenMenu(IPlayer caller, string menuKey) =>
        OpenMenu(new MenuOpenRequest { Caller = caller, MenuKey = menuKey });

    internal MenuOperationResult OpenMenu(MenuOpenRequest request)
    {
        if (request?.Caller is null || string.IsNullOrWhiteSpace(request.MenuKey))
        {
            return Failure(MenuOperationStatus.InvalidRequest, "menu_request_invalid");
        }

        return OpenMenu(request, snapshots.Current);
    }

    internal MenuOperationResult OpenMenu(
        IPlayer caller,
        MenuRuntimeSnapshot snapshot,
        string menuKey) =>
        OpenMenu(new MenuOpenRequest { Caller = caller, MenuKey = menuKey }, snapshot);

    MenuOperationResult IMenuCommandTarget.OpenMenu(
        IPlayer caller,
        MenuRuntimeSnapshot snapshot,
        string menuKey) => OpenMenu(caller, snapshot, menuKey);

    internal MenuOperationResult OpenProviderMenu(
        IPlayer caller,
        string providerKey,
        string menuKey)
    {
        if (!IsEligible(caller)
            || string.IsNullOrWhiteSpace(providerKey)
            || string.IsNullOrWhiteSpace(menuKey))
        {
            return Failure(MenuOperationStatus.InvalidRequest, "provider_menu_request_invalid");
        }

        return providers.InvokeMenu(
            providerKey,
            menuKey,
            new MenuProviderInvocationContext(caller, caller, EmptyArguments, 0));
    }

    private MenuOperationResult OpenMenu(MenuOpenRequest request, MenuRuntimeSnapshot snapshot)
    {
        var caller = request.Caller;
        if (!IsEligible(caller))
        {
            return Failure(MenuOperationStatus.InvalidRequest, "caller_invalid");
        }

        if (!snapshot.TryGetMenu(request.MenuKey, out var compiled))
        {
            return Failure(MenuOperationStatus.NotFound, "menu_not_found");
        }

        if (!accessResolver.CanAccess(caller, compiled.Definition.Access))
        {
            return Failure(MenuOperationStatus.AccessDenied, "menu_access_denied");
        }

        var audience = request.AudienceOverride ?? compiled.Definition.Audience;
        var resolution = audienceResolver.Resolve(caller, audience, request.ExplicitTargets);
        if (!resolution.IsAllowed)
        {
            return Failure(MenuOperationStatus.AccessDenied, resolution.ErrorCode ?? "audience_denied");
        }

        var opened = 0;
        MenuOperationResult? lastFailure = null;
        foreach (var target in resolution.Targets)
        {
            if (!accessResolver.CanAccess(target, compiled.Definition.Access))
            {
                continue;
            }

            var result = OpenCompiledMenu(snapshot, caller, target, compiled, audience, depth: 0);
            if (result.IsSuccess) opened++;
            else lastFailure = result;
        }

        return opened > 0
            ? MenuOperationResult.Succeeded
            : lastFailure ?? Failure(MenuOperationStatus.AccessDenied, "audience_empty_or_denied");
    }

    private MenuOperationResult OpenCompiledMenu(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        CompiledMenu menu,
        MenuAudienceDefinition invocationAudience,
        int depth)
    {
        if (depth > _configuration.MaxNavigationDepth)
        {
            return Failure(MenuOperationStatus.ValidationFailed, "navigation_depth_exceeded");
        }

        if (!IsEligible(target) || !accessResolver.CanAccess(target, menu.Definition.Access))
        {
            return Failure(MenuOperationStatus.AccessDenied, "menu_access_denied");
        }

        return adapter.Open(CreateRenderRequest(snapshot, caller, target, menu, invocationAudience, depth));
    }

    private MenuRenderRequest CreateRenderRequest(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        CompiledMenu menu,
        MenuAudienceDefinition invocationAudience,
        int depth)
    {
        return new MenuRenderRequest
        {
            Caller = caller,
            Target = target,
            Menu = menu.Definition,
            InvocationAudience = invocationAudience,
            Locale = _configuration.DefaultLocale,
            Depth = depth,
            IsActionAvailable = action => IsActionAvailable(snapshot, menu, action),
            Execute = (item, action, value) =>
                ExecuteAction(snapshot, caller, target, menu, invocationAudience, item, action, value, depth),
            BuildParent = (reference, parentDepth) =>
                BuildParent(snapshot, caller, target, reference, invocationAudience, parentDepth),
        };
    }

    private IMenuAPI? BuildParent(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        MenuReferenceDefinition reference,
        MenuAudienceDefinition invocationAudience,
        int depth)
    {
        if (depth > _configuration.MaxNavigationDepth
            || reference.ProviderKey is not null
            || !snapshot.TryGetMenu(reference.MenuKey, out var parent)
            || !accessResolver.CanAccess(target, parent.Definition.Access))
        {
            return null;
        }

        return adapter.Build(CreateRenderRequest(snapshot, caller, target, parent, invocationAudience, depth));
    }

    private MenuOperationResult ExecuteAction(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        CompiledMenu currentMenu,
        MenuAudienceDefinition invocationAudience,
        MenuItemDefinition? item,
        MenuActionDefinition action,
        JsonElement? changedValue,
        int currentDepth)
    {
        if (currentDepth > _configuration.MaxNavigationDepth)
        {
            return Failure(MenuOperationStatus.ValidationFailed, "navigation_depth_exceeded");
        }

        if (!IsEligible(caller)
            || !IsEligible(target)
            || !accessResolver.CanAccess(caller, currentMenu.Definition.Access)
            || !audienceResolver.CanInvoke(caller, invocationAudience)
            || !accessResolver.CanAccess(target, currentMenu.Definition.Access)
            || item is not null
            && !accessResolver.CanAccess(target, item.Access, currentMenu.Definition.Access))
        {
            target.SendChat("[Menu] Нет доступа.");
            return Failure(MenuOperationStatus.AccessDenied, "action_access_denied");
        }

        return action.Kind switch
        {
            MenuActionKind.None => MenuOperationResult.Succeeded,
            MenuActionKind.OpenMenu => TryAdvance(currentDepth, out var menuDepth)
                ? OpenReleaseMenuAction(snapshot, caller, target, action, invocationAudience, menuDepth)
                : DepthExceeded(),
            MenuActionKind.OpenProviderMenu => TryAdvance(currentDepth, out var providerMenuDepth)
                ? OpenProviderMenuAction(caller, target, action, providerMenuDepth)
                : DepthExceeded(),
            MenuActionKind.ProviderAction => InvokeProviderAction(
                caller, target, action, changedValue, currentDepth),
            MenuActionKind.Back => OpenBackAction(
                snapshot,
                caller,
                target,
                currentMenu,
                invocationAudience,
                MenuNavigationDepthGuard.Back(currentDepth)),
            MenuActionKind.Close => Close(target),
            _ => Failure(MenuOperationStatus.Unsupported, "action_kind_unsupported"),
        };
    }

    private MenuOperationResult OpenReleaseMenuAction(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        MenuActionDefinition action,
        MenuAudienceDefinition invocationAudience,
        int depth)
    {
        var reference = action.TargetMenu;
        if (reference is null
            || reference.ProviderKey is not null
            || !snapshot.TryGetMenu(reference.MenuKey, out var next))
        {
            return Failure(MenuOperationStatus.NotFound, "target_menu_not_found");
        }

        return OpenCompiledMenu(snapshot, caller, target, next, invocationAudience, depth);
    }

    private MenuOperationResult OpenProviderMenuAction(
        IPlayer caller,
        IPlayer target,
        MenuActionDefinition action,
        int depth)
    {
        var reference = action.TargetMenu;
        if (reference?.ProviderKey is null)
        {
            return Failure(MenuOperationStatus.InvalidRequest, "provider_menu_reference_invalid");
        }

        return providers.InvokeMenu(
            reference.ProviderKey,
            reference.MenuKey,
            new MenuProviderInvocationContext(caller, target, EmptyArguments, depth));
    }

    private MenuOperationResult InvokeProviderAction(
        IPlayer caller,
        IPlayer target,
        MenuActionDefinition action,
        JsonElement? changedValue,
        int depth)
    {
        if (action.ProviderKey is null || action.ProviderActionKey is null)
        {
            return Failure(MenuOperationStatus.InvalidRequest, "provider_action_reference_invalid");
        }

        var arguments = MenuActionArguments.Compose(action.Arguments, changedValue);
        return providers.InvokeAction(
            action.ProviderKey,
            action.ProviderActionKey,
            new MenuProviderInvocationContext(caller, target, arguments, depth));
    }

    private MenuOperationResult OpenBackAction(
        MenuRuntimeSnapshot snapshot,
        IPlayer caller,
        IPlayer target,
        CompiledMenu currentMenu,
        MenuAudienceDefinition invocationAudience,
        int depth)
    {
        var parent = currentMenu.Definition.Parent;
        if (parent is null)
        {
            return Failure(MenuOperationStatus.NotFound, "parent_menu_not_configured");
        }

        if (parent.ProviderKey is { } providerKey)
        {
            return providers.InvokeMenu(
                providerKey,
                parent.MenuKey,
                new MenuProviderInvocationContext(caller, target, EmptyArguments, depth));
        }

        return snapshot.TryGetMenu(parent.MenuKey, out var definition)
            ? OpenCompiledMenu(snapshot, caller, target, definition, invocationAudience, depth)
            : Failure(MenuOperationStatus.NotFound, "parent_menu_not_found");
    }

    private MenuOperationResult Close(IPlayer target)
    {
        core.MenusAPI.CloseActiveMenu(target);
        return MenuOperationResult.Succeeded;
    }

    private bool IsActionAvailable(
        MenuRuntimeSnapshot snapshot,
        CompiledMenu currentMenu,
        MenuActionDefinition action)
    {
        return action.Kind switch
        {
            MenuActionKind.None or MenuActionKind.Close => true,
            MenuActionKind.OpenMenu =>
                action.TargetMenu is { ProviderKey: null } reference
                && snapshot.Menus.ContainsKey(reference.MenuKey),
            MenuActionKind.OpenProviderMenu =>
                action.TargetMenu is { ProviderKey: not null } reference
                && providers.IsMenuAvailable(reference.ProviderKey, reference.MenuKey),
            MenuActionKind.ProviderAction =>
                action.ProviderKey is not null
                && action.ProviderActionKey is not null
                && providers.IsActionAvailable(action.ProviderKey, action.ProviderActionKey),
            MenuActionKind.Back => currentMenu.Definition.Parent is { } parent
                && (parent.ProviderKey is { } providerKey
                    ? providers.IsMenuAvailable(providerKey, parent.MenuKey)
                    : snapshot.Menus.ContainsKey(parent.MenuKey)),
            _ => false,
        };
    }

    private static MenuOperationResult Failure(MenuOperationStatus status, string code) =>
        MenuOperationResult.Failure(status, code);

    private bool TryAdvance(int currentDepth, out int nextDepth) =>
        MenuNavigationDepthGuard.TryAdvance(
            currentDepth,
            _configuration.MaxNavigationDepth,
            out nextDepth);

    private static MenuOperationResult DepthExceeded() =>
        Failure(MenuOperationStatus.ValidationFailed, "navigation_depth_exceeded");

    private static bool IsEligible(IPlayer? player) =>
        player is { IsValid: true, IsAuthorized: true, IsFakeClient: false };
}
