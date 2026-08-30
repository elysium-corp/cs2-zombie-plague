using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Providers;
using Menu.Api.Results;
using Menu.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Menu.Core.Providers;

/// <summary>
/// Хранит только делегаты текущих загрузок Provider и исключает stale handles.
/// </summary>
internal sealed partial class ProviderRegistry(
    IProviderStateSink stateSink,
    ILogger<ProviderRegistry> logger)
{
    private const int MaximumDisplayNameLength = 128;
    private const int MaximumPluginVersionLength = 32;
    private const int MaximumLocalizedTextLength = 2_048;
    private const int MaximumTranslations = 64;
    private readonly ConcurrentDictionary<string, ProviderSession> _providers =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, RejectedProviderSession> _rejectedProviders =
        new(StringComparer.Ordinal);
    private readonly object _lifecycleGate = new();
    private long _generation;
    private bool _stopped;

    internal IMenuProviderRegistration Register(MenuProviderDescriptor descriptor)
    {
        var validation = ValidateProviderDescriptor(descriptor);
        if (!validation.IsSuccess)
        {
            return new RejectedProviderRegistration(descriptor?.ProviderKey ?? string.Empty, validation);
        }

        try
        {
            descriptor = CloneProviderDescriptor(descriptor);
        }
        catch (Exception)
        {
            return new RejectedProviderRegistration(
                descriptor.ProviderKey,
                MenuOperationResult.Failure(
                    MenuOperationStatus.InvalidRequest,
                    "provider_descriptor_json_invalid"));
        }

        if (descriptor.MenuApiVersion != MenuContractVersions.MenuCoreApiVersion)
        {
            return RegisterIncompatibleProvider(descriptor);
        }

        ProviderSession? previous;
        ProviderSession session;
        lock (_lifecycleGate)
        {
            if (_stopped)
            {
                return new RejectedProviderRegistration(
                    descriptor.ProviderKey,
                    MenuOperationResult.Failure(
                        MenuOperationStatus.Disposed,
                        "provider_registry_stopped"));
            }

            session = new ProviderSession(
                descriptor,
                Guid.NewGuid(),
                Interlocked.Increment(ref _generation));

            _rejectedProviders.Remove(descriptor.ProviderKey);
            _providers.TryGetValue(descriptor.ProviderKey, out previous);
            _providers[descriptor.ProviderKey] = session;

            // Событие ставится в очередь под тем же lifecycle lock: последовательные
            // hot reload одного provider_key не могут поменяться местами в БД.
            TryPersist(() => stateSink.ProviderRegistered(
                descriptor,
                session.SessionId,
                session.Generation));

        }

        // Provider callbacks are third-party code. Deactivation waits for callbacks
        // already in flight, therefore it must never happen under the lifecycle lock:
        // a callback is allowed to unregister or attempt a new registration itself.
        previous?.Deactivate();
        return new ProviderRegistration(this, session);
    }

    private IMenuProviderRegistration RegisterIncompatibleProvider(MenuProviderDescriptor descriptor)
    {
        var result = MenuOperationResult.Failure(
            MenuOperationStatus.Unsupported,
            "provider_api_incompatible");
        ProviderSession? previous;
        Guid sessionId;
        long generation;
        lock (_lifecycleGate)
        {
            if (_stopped)
            {
                return new RejectedProviderRegistration(
                    descriptor.ProviderKey,
                    MenuOperationResult.Failure(
                        MenuOperationStatus.Disposed,
                        "provider_registry_stopped"));
            }

            sessionId = Guid.NewGuid();
            generation = Interlocked.Increment(ref _generation);
            _providers.TryRemove(descriptor.ProviderKey, out previous);
            var status = descriptor.MenuApiVersion < MenuContractVersions.MenuCoreApiVersion
                ? ProviderRejectionStatus.ApiOutdated
                : ProviderRejectionStatus.Incompatible;
            _rejectedProviders[descriptor.ProviderKey] = new RejectedProviderSession(
                descriptor,
                sessionId,
                generation);
            TryPersist(() => stateSink.ProviderRejected(
                descriptor,
                sessionId,
                generation,
                status,
                result.Code ?? "provider_api_incompatible"));
        }

        previous?.Deactivate();
        return new RejectedProviderRegistration(
            this,
            descriptor.ProviderKey,
            result,
            sessionId,
            generation);
    }

    internal bool IsProviderOnline(string providerKey) =>
        TryGetCurrent(providerKey, out var session) && session.IsActive;

    internal bool IsMenuAvailable(string providerKey, string menuKey) =>
        TryGetCurrent(providerKey, out var session) && session.ContainsMenu(menuKey);

    internal bool IsActionAvailable(string providerKey, string actionKey) =>
        TryGetCurrent(providerKey, out var session) && session.ContainsAction(actionKey);

    internal MenuOperationResult InvokeMenu(
        string providerKey,
        string menuKey,
        MenuProviderInvocationContext context)
    {
        if (!TryGetCurrent(providerKey, out var session))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.ProviderOffline,
                "provider_offline");
        }

        return session.InvokeMenu(menuKey, context, logger);
    }

    internal MenuOperationResult InvokeAction(
        string providerKey,
        string actionKey,
        MenuProviderInvocationContext context)
    {
        if (!TryGetCurrent(providerKey, out var session))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.ProviderOffline,
                "provider_offline");
        }

        return session.InvokeAction(actionKey, context, logger);
    }

    internal IReadOnlyList<MenuProviderDescriptor> GetOnlineProviders() =>
        _providers.Values
            .Where(session => session.IsActive)
            .Select(session => session.Descriptor)
            .OrderBy(descriptor => descriptor.ProviderKey, StringComparer.Ordinal)
            .ToArray();

    internal ProviderValidationCatalog BuildValidationCatalog()
    {
        var entries = new List<ProviderValidationEntry>();
        lock (_lifecycleGate)
        {
            foreach (var session in _providers.Values)
            {
                var entry = session.TryCreateValidationEntry();
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }

            entries.AddRange(_rejectedProviders.Values.Select(static registration =>
                new ProviderValidationEntry(
                    registration.Descriptor.ProviderKey,
                    registration.Descriptor.MenuApiVersion,
                    ProviderAvailability.Incompatible,
                    Array.Empty<string>(),
                    Array.Empty<string>())));
        }

        return new ProviderValidationCatalog(entries);
    }

    internal void Stop()
    {
        ProviderSession[] sessions;
        lock (_lifecycleGate)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            var removed = new List<ProviderSession>();
            foreach (var pair in _providers.ToArray())
            {
                if (_providers.TryRemove(pair.Key, out var session))
                {
                    removed.Add(session);
                    TryPersist(() => stateSink.ProviderOffline(
                        pair.Key,
                        session.SessionId,
                        session.Generation));
                }
            }

            foreach (var pair in _rejectedProviders)
            {
                TryPersist(() => stateSink.ProviderOffline(
                    pair.Key,
                    pair.Value.SessionId,
                    pair.Value.Generation));
            }
            _rejectedProviders.Clear();

            sessions = removed.ToArray();
        }

        foreach (var session in sessions)
        {
            session.Deactivate();
        }
    }

    private bool TryGetCurrent(string providerKey, out ProviderSession session)
    {
        session = null!;
        return !string.IsNullOrWhiteSpace(providerKey)
               && _providers.TryGetValue(providerKey, out session)
               && session.IsActive;
    }

    private bool IsCurrent(ProviderSession session) =>
        _providers.TryGetValue(session.Descriptor.ProviderKey, out var current)
        && ReferenceEquals(current, session)
        && session.IsActive;

    private MenuOperationResult RegisterMenu(
        ProviderSession session,
        MenuProviderMenuDescriptor descriptor)
    {
        if (!IsCurrent(session))
        {
            return DisposedResult();
        }

        if (!IsTechnicalIdentifier(descriptor?.MenuKey)
            || descriptor.Handler is null
            || !IsLocalizedTextValid(
                descriptor.DisplayName,
                required: true,
                maximumDefaultLength: MaximumDisplayNameLength)
            || descriptor.Description is not null
               && !IsLocalizedTextValid(
                   descriptor.Description,
                   required: false,
                   maximumDefaultLength: MaximumLocalizedTextLength)
            || !IsMetadataValid(descriptor.Metadata))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidRequest,
                "provider_menu_invalid");
        }

        try
        {
            descriptor = CloneMenuDescriptor(descriptor);
        }
        catch (Exception)
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidRequest,
                "provider_menu_json_invalid");
        }

        var result = session.RegisterMenu(descriptor);
        if (result.IsSuccess)
        {
            TryPersist(() => stateSink.MenuDeclared(
                session.Descriptor.ProviderKey,
                session.SessionId,
                session.Generation,
                descriptor));
        }

        return result;
    }

    private MenuOperationResult RegisterAction(
        ProviderSession session,
        MenuProviderActionDescriptor descriptor)
    {
        if (!IsCurrent(session))
        {
            return DisposedResult();
        }

        if (!IsTechnicalIdentifier(descriptor?.ActionKey)
            || descriptor.Validator is null
            || descriptor.Handler is null
            || !IsLocalizedTextValid(
                descriptor.DisplayName,
                required: true,
                maximumDefaultLength: MaximumDisplayNameLength)
            || descriptor.Description is not null
               && !IsLocalizedTextValid(
                   descriptor.Description,
                   required: false,
                   maximumDefaultLength: MaximumLocalizedTextLength)
            || descriptor.ArgumentsSchema is { ValueKind: not JsonValueKind.Object }
            || descriptor.ArgumentsSchema is { } schema && ContainsNul(schema)
            || !IsMetadataValid(descriptor.Metadata))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidRequest,
                "provider_action_invalid");
        }

        try
        {
            descriptor = CloneActionDescriptor(descriptor);
        }
        catch (Exception)
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidRequest,
                "provider_action_json_invalid");
        }

        var result = session.RegisterAction(descriptor);
        if (result.IsSuccess)
        {
            TryPersist(() => stateSink.ActionDeclared(
                session.Descriptor.ProviderKey,
                session.SessionId,
                session.Generation,
                descriptor));
        }

        return result;
    }

    private MenuOperationResult UnregisterExport(
        ProviderSession session,
        string exportType,
        string exportKey)
    {
        if (!IsCurrent(session))
        {
            return DisposedResult();
        }

        if (!IsTechnicalIdentifier(exportKey))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidIdentifier,
                "export_key_invalid");
        }

        var removed = exportType == "menu"
            ? session.UnregisterMenu(exportKey)
            : session.UnregisterAction(exportKey);
        if (!removed)
        {
            return MenuOperationResult.Failure(MenuOperationStatus.NotFound, "export_not_found");
        }

        TryPersist(() => stateSink.ExportRemoved(
            session.Descriptor.ProviderKey,
            session.SessionId,
            session.Generation,
            exportType,
            exportKey));
        return MenuOperationResult.Succeeded;
    }

    private MenuOperationResult Unregister(ProviderSession session)
    {
        var removed = false;
        lock (_lifecycleGate)
        {
            if (_providers.TryGetValue(session.Descriptor.ProviderKey, out var current)
                && ReferenceEquals(current, session)
                && ((ICollection<KeyValuePair<string, ProviderSession>>)_providers).Remove(
                    new KeyValuePair<string, ProviderSession>(session.Descriptor.ProviderKey, session)))
            {
                removed = true;
                TryPersist(() => stateSink.ProviderOffline(
                    session.Descriptor.ProviderKey,
                    session.SessionId,
                    session.Generation));
            }
        }

        session.Deactivate();
        return removed ? MenuOperationResult.Succeeded : DisposedResult();
    }

    private static MenuOperationResult ValidateProviderDescriptor(MenuProviderDescriptor? descriptor)
    {
        if (descriptor is null
            || !IsTechnicalIdentifier(descriptor.ProviderKey)
            || !IsPlainTextValid(descriptor.DisplayName, MaximumDisplayNameLength)
            || !IsPlainTextValid(descriptor.PluginVersion, MaximumPluginVersionLength)
            || descriptor.MenuApiVersion <= 0
            || !AreCapabilitiesValid(descriptor.Capabilities)
            || !IsMetadataValid(descriptor.Metadata))
        {
            return MenuOperationResult.Failure(
                MenuOperationStatus.InvalidIdentifier,
                "provider_descriptor_invalid");
        }

        return MenuOperationResult.Succeeded;
    }

    private static bool IsPlainTextValid(string? value, int maximumLength, bool required = true) =>
        value is not null
        && (!required || !string.IsNullOrWhiteSpace(value))
        && value.Length <= maximumLength
        && !value.Contains('\0');

    private static bool IsLocalizedTextValid(
        LocalizedText? value,
        bool required,
        int maximumDefaultLength)
    {
        if (value is null
            || !IsPlainTextValid(value.Default, maximumDefaultLength, required)
            || value.Translations is null
            || value.Translations.Count > MaximumTranslations)
        {
            return false;
        }

        return value.Translations.All(static translation =>
            !string.IsNullOrEmpty(translation.Key)
            && !translation.Key.Contains('\0')
            && IsPlainTextValid(
                translation.Value,
                MaximumLocalizedTextLength,
                required: false));
    }

    private static bool AreCapabilitiesValid(IReadOnlyList<string>? capabilities)
    {
        if (capabilities is null)
        {
            return true;
        }

        return capabilities.All(IsTechnicalIdentifier)
               && capabilities.Distinct(StringComparer.Ordinal).Count() == capabilities.Count;
    }

    private static bool IsMetadataValid(IReadOnlyDictionary<string, JsonElement>? metadata)
    {
        if (metadata is null)
        {
            return true;
        }

        return metadata.All(static pair =>
            !string.IsNullOrEmpty(pair.Key)
            && !pair.Key.Contains('\0')
            && !ContainsNul(pair.Value));
    }

    private static bool ContainsNul(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Contains('\0') == true,
            JsonValueKind.Array => value.EnumerateArray().Any(ContainsNul),
            JsonValueKind.Object => value.EnumerateObject().Any(static property =>
                property.Name.Contains('\0') || ContainsNul(property.Value)),
            _ => false
        };
    }

    private static MenuProviderDescriptor CloneProviderDescriptor(MenuProviderDescriptor descriptor) =>
        descriptor with
        {
            Capabilities = descriptor.Capabilities?.ToArray() ?? Array.Empty<string>(),
            Metadata = CloneMetadata(descriptor.Metadata),
        };

    private static MenuProviderMenuDescriptor CloneMenuDescriptor(MenuProviderMenuDescriptor descriptor) =>
        descriptor with
        {
            DisplayName = CloneLocalizedText(descriptor.DisplayName),
            Description = descriptor.Description is null ? null : CloneLocalizedText(descriptor.Description),
            Metadata = CloneMetadata(descriptor.Metadata),
        };

    private static MenuProviderActionDescriptor CloneActionDescriptor(MenuProviderActionDescriptor descriptor) =>
        descriptor with
        {
            DisplayName = CloneLocalizedText(descriptor.DisplayName),
            Description = descriptor.Description is null ? null : CloneLocalizedText(descriptor.Description),
            ArgumentsSchema = descriptor.ArgumentsSchema?.Clone(),
            Metadata = CloneMetadata(descriptor.Metadata),
        };

    private static LocalizedText CloneLocalizedText(LocalizedText value) =>
        value with
        {
            Translations = value.Translations is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(value.Translations, StringComparer.Ordinal),
        };

    private static IReadOnlyDictionary<string, JsonElement> CloneMetadata(
        IReadOnlyDictionary<string, JsonElement>? metadata) =>
        metadata is null
            ? new Dictionary<string, JsonElement>()
            : metadata.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.Clone(),
                StringComparer.Ordinal);

    private static bool IsTechnicalIdentifier(string? value) =>
        value is not null
        && value.Length <= MenuContractVersions.MaxTechnicalIdentifierLength
        && TechnicalIdentifierRegex().IsMatch(value);

    private static MenuOperationResult DisposedResult() =>
        MenuOperationResult.Failure(MenuOperationStatus.Disposed, "provider_registration_disposed");

    private void UnregisterRejected(string providerKey, Guid sessionId, long generation)
    {
        lock (_lifecycleGate)
        {
            if (_rejectedProviders.TryGetValue(providerKey, out var current)
                && current.SessionId == sessionId
                && current.Generation == generation)
            {
                _rejectedProviders.Remove(providerKey);
                TryPersist(() => stateSink.ProviderOffline(providerKey, sessionId, generation));
            }
        }
    }

    private void TryPersist(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Не удалось поставить Provider registry event в persistence queue.");
        }
    }

    [GeneratedRegex(MenuContractVersions.TechnicalIdentifierPattern, RegexOptions.CultureInvariant)]
    private static partial Regex TechnicalIdentifierRegex();

    private sealed record RejectedProviderSession(
        MenuProviderDescriptor Descriptor,
        Guid SessionId,
        long Generation);

    private sealed class ProviderRegistration(
        ProviderRegistry owner,
        ProviderSession session) : IMenuProviderRegistration
    {
        private int _disposed;

        public string ProviderKey => session.Descriptor.ProviderKey;

        public bool IsRegistered => Volatile.Read(ref _disposed) == 0 && owner.IsCurrent(session);

        public MenuOperationResult RegistrationResult => MenuOperationResult.Succeeded;

        public MenuOperationResult RegisterMenu(MenuProviderMenuDescriptor descriptor) =>
            Volatile.Read(ref _disposed) == 0
                ? owner.RegisterMenu(session, descriptor)
                : DisposedResult();

        public MenuOperationResult RegisterAction(MenuProviderActionDescriptor descriptor) =>
            Volatile.Read(ref _disposed) == 0
                ? owner.RegisterAction(session, descriptor)
                : DisposedResult();

        public MenuOperationResult UnregisterMenu(string menuKey) =>
            Volatile.Read(ref _disposed) == 0
                ? owner.UnregisterExport(session, "menu", menuKey)
                : DisposedResult();

        public MenuOperationResult UnregisterAction(string actionKey) =>
            Volatile.Read(ref _disposed) == 0
                ? owner.UnregisterExport(session, "action", actionKey)
                : DisposedResult();

        public MenuOperationResult UnregisterProvider()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return DisposedResult();
            }

            return owner.Unregister(session);
        }

        public void Dispose() => _ = UnregisterProvider();
    }

    private sealed class RejectedProviderRegistration : IMenuProviderRegistration
    {
        private readonly ProviderRegistry? _owner;
        private readonly Guid _sessionId;
        private readonly long _generation;
        private int _disposed;

        internal RejectedProviderRegistration(string providerKey, MenuOperationResult result)
        {
            ProviderKey = providerKey;
            RegistrationResult = result;
        }

        internal RejectedProviderRegistration(
            ProviderRegistry owner,
            string providerKey,
            MenuOperationResult result,
            Guid sessionId,
            long generation)
            : this(providerKey, result)
        {
            _owner = owner;
            _sessionId = sessionId;
            _generation = generation;
        }

        public string ProviderKey { get; }
        public bool IsRegistered => false;
        public MenuOperationResult RegistrationResult { get; }
        public MenuOperationResult RegisterMenu(MenuProviderMenuDescriptor descriptor) => RegistrationResult;
        public MenuOperationResult RegisterAction(MenuProviderActionDescriptor descriptor) => RegistrationResult;
        public MenuOperationResult UnregisterMenu(string menuKey) => RegistrationResult;
        public MenuOperationResult UnregisterAction(string actionKey) => RegistrationResult;

        public MenuOperationResult UnregisterProvider()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0 && _owner is not null)
            {
                _owner.UnregisterRejected(ProviderKey, _sessionId, _generation);
            }

            return DisposedResult();
        }

        public void Dispose() => _ = UnregisterProvider();
    }

    private sealed class ProviderSession(
        MenuProviderDescriptor descriptor,
        Guid sessionId,
        long generation)
    {
        private readonly ReaderWriterLockSlim _gate = new(LockRecursionPolicy.NoRecursion);
        private readonly object _invocationGate = new();
        private readonly Dictionary<string, MenuProviderMenuDescriptor> _menus = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MenuProviderActionDescriptor> _actions = new(StringComparer.Ordinal);
        private readonly Dictionary<int, int> _invocationsByThread = [];
        private int _inFlightInvocations;
        private int _active = 1;

        internal MenuProviderDescriptor Descriptor { get; } = descriptor;
        internal Guid SessionId { get; } = sessionId;
        internal long Generation { get; } = generation;
        internal bool IsActive => Volatile.Read(ref _active) != 0;

        internal bool ContainsMenu(string menuKey) => Read(() => _menus.ContainsKey(menuKey));
        internal bool ContainsAction(string actionKey) => Read(() => _actions.ContainsKey(actionKey));

        internal MenuOperationResult RegisterMenu(MenuProviderMenuDescriptor value) =>
            Write(() =>
            {
                if (!IsActive) return DisposedResult();
                _menus[value.MenuKey] = value;
                return MenuOperationResult.Succeeded;
            });

        internal MenuOperationResult RegisterAction(MenuProviderActionDescriptor value) =>
            Write(() =>
            {
                if (!IsActive) return DisposedResult();
                _actions[value.ActionKey] = value;
                return MenuOperationResult.Succeeded;
            });

        internal bool UnregisterMenu(string menuKey) => Write(() => _menus.Remove(menuKey));
        internal bool UnregisterAction(string actionKey) => Write(() => _actions.Remove(actionKey));

        internal MenuOperationResult InvokeMenu(
            string menuKey,
            MenuProviderInvocationContext context,
            ILogger logger)
        {
            if (!TryBeginMenuInvocation(menuKey, out var export, out var failure))
            {
                return failure;
            }

            try
            {
                return export.Handler(context)
                       ?? MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "provider_menu_null_result");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Provider {ProviderKey} menu {MenuKey} завершилось ошибкой.",
                    Descriptor.ProviderKey, menuKey);
                return MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "provider_menu_failed");
            }
            finally
            {
                EndInvocation();
            }
        }

        internal MenuOperationResult InvokeAction(
            string actionKey,
            MenuProviderInvocationContext context,
            ILogger logger)
        {
            if (!TryBeginActionInvocation(actionKey, out var export, out var failure))
            {
                return failure;
            }

            try
            {
                if (export.ArgumentsSchema is { } schema)
                {
                    var schemaValidation = ProviderJsonSchemaValidator.Validate(context.Arguments, schema);
                    if (!schemaValidation.IsValid)
                    {
                        return MenuOperationResult.Failure(
                            MenuOperationStatus.ValidationFailed,
                            "provider_schema_rejected",
                            issues: schemaValidation.Issues);
                    }
                }

                var validation = export.Validator(context.Arguments);
                if (validation is null || !validation.IsValid)
                {
                    return MenuOperationResult.Failure(
                        MenuOperationStatus.ValidationFailed,
                        "provider_action_rejected",
                        issues: validation?.Issues);
                }

                return export.Handler(context)
                       ?? MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "provider_action_null_result");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Provider {ProviderKey} action {ActionKey} завершилось ошибкой.",
                    Descriptor.ProviderKey, actionKey);
                return MenuOperationResult.Failure(MenuOperationStatus.HandlerFailed, "provider_action_failed");
            }
            finally
            {
                EndInvocation();
            }
        }

        internal ProviderValidationEntry? TryCreateValidationEntry() => Read<ProviderValidationEntry?>(() =>
        {
            if (!IsActive)
            {
                return null;
            }

            var actionKeys = _actions.Keys.ToArray();
            var validators = actionKeys.ToDictionary(
                static actionKey => actionKey,
                actionKey => new ProviderArgumentValidator(arguments =>
                    ValidateActionArguments(actionKey, arguments)),
                StringComparer.Ordinal);
            var schemas = _actions
                .Where(static pair => pair.Value.ArgumentsSchema is not null)
                .ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value.ArgumentsSchema!.Value.Clone(),
                    StringComparer.Ordinal);

            return new ProviderValidationEntry(
                Descriptor.ProviderKey,
                Descriptor.MenuApiVersion,
                ProviderAvailability.Online,
                _menus.Keys.ToArray(),
                actionKeys,
                validators,
                schemas);
        });

        internal void Deactivate()
        {
            _gate.EnterWriteLock();
            try
            {
                if (Interlocked.Exchange(ref _active, 0) != 0)
                {
                    _menus.Clear();
                    _actions.Clear();
                }
            }
            finally
            {
                _gate.ExitWriteLock();
            }

            WaitForOtherInvocations();
        }

        private bool TryBeginMenuInvocation(
            string menuKey,
            out MenuProviderMenuDescriptor export,
            out MenuOperationResult failure)
        {
            _gate.EnterReadLock();
            try
            {
                if (!IsActive)
                {
                    export = null!;
                    failure = MenuOperationResult.Failure(
                        MenuOperationStatus.ProviderOffline,
                        "provider_offline");
                    return false;
                }

                if (!_menus.TryGetValue(menuKey, out export!))
                {
                    failure = MenuOperationResult.Failure(
                        MenuOperationStatus.NotFound,
                        "provider_menu_not_found");
                    return false;
                }

                BeginInvocation();
                failure = MenuOperationResult.Succeeded;
                return true;
            }
            finally
            {
                _gate.ExitReadLock();
            }
        }

        private bool TryBeginActionInvocation(
            string actionKey,
            out MenuProviderActionDescriptor export,
            out MenuOperationResult failure)
        {
            _gate.EnterReadLock();
            try
            {
                if (!IsActive)
                {
                    export = null!;
                    failure = MenuOperationResult.Failure(
                        MenuOperationStatus.ProviderOffline,
                        "provider_offline");
                    return false;
                }

                if (!_actions.TryGetValue(actionKey, out export!))
                {
                    failure = MenuOperationResult.Failure(
                        MenuOperationStatus.NotFound,
                        "provider_action_not_found");
                    return false;
                }

                BeginInvocation();
                failure = MenuOperationResult.Succeeded;
                return true;
            }
            finally
            {
                _gate.ExitReadLock();
            }
        }

        private MenuValidationResult ValidateActionArguments(string actionKey, JsonElement? arguments)
        {
            if (!TryBeginActionInvocation(actionKey, out var export, out var failure))
            {
                return MenuValidationResult.Invalid(
                    failure.Code ?? "provider_action_unavailable",
                    "Provider action is no longer available.");
            }

            try
            {
                var value = arguments ?? JsonSerializer.SerializeToElement(new { });
                return export.Validator(value)
                       ?? MenuValidationResult.Invalid(
                           "provider_action_null_validation",
                           "Provider validator returned null.");
            }
            catch (Exception exception)
            {
                return MenuValidationResult.Invalid(
                    "provider_validator_failed",
                    exception.GetType().Name);
            }
            finally
            {
                EndInvocation();
            }
        }

        private void BeginInvocation()
        {
            var threadId = Environment.CurrentManagedThreadId;
            lock (_invocationGate)
            {
                _inFlightInvocations++;
                _invocationsByThread.TryGetValue(threadId, out var current);
                _invocationsByThread[threadId] = current + 1;
            }
        }

        private void EndInvocation()
        {
            var threadId = Environment.CurrentManagedThreadId;
            lock (_invocationGate)
            {
                _inFlightInvocations--;
                var current = _invocationsByThread[threadId] - 1;
                if (current == 0)
                {
                    _invocationsByThread.Remove(threadId);
                }
                else
                {
                    _invocationsByThread[threadId] = current;
                }

                Monitor.PulseAll(_invocationGate);
            }
        }

        private void WaitForOtherInvocations()
        {
            var threadId = Environment.CurrentManagedThreadId;
            lock (_invocationGate)
            {
                _invocationsByThread.TryGetValue(threadId, out var currentThreadInvocations);
                while (_inFlightInvocations > currentThreadInvocations)
                {
                    Monitor.Wait(_invocationGate);
                }
            }
        }

        private T Read<T>(Func<T> action)
        {
            _gate.EnterReadLock();
            try { return action(); }
            finally { _gate.ExitReadLock(); }
        }

        private T Write<T>(Func<T> action)
        {
            _gate.EnterWriteLock();
            try { return action(); }
            finally { _gate.ExitWriteLock(); }
        }
    }
}
