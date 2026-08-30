using Menu.Api.Enums;
using Menu.Api.Results;
using Menu.Core.Runtime;
using Menu.Core.Validation;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Commands;

/// <summary>
/// Маршрутизирует опубликованные chat и console aliases через активный snapshot.
/// </summary>
/// <remarks>
/// Chat aliases обслуживаются одним синхронным <c>HookClientChat</c>. Это важно
/// для режима <see cref="ChatSuppressionMode.OnSuccess"/>: обычный command callback
/// вызывается слишком поздно, чтобы принять решение о показе исходного сообщения.
/// Slash aliases поддерживаются контрактом, однако CS2 или другой plugin может
/// перехватить строку с <c>/</c> до Swiftly <c>HookClientChat</c>; при активации
/// такого Release пишется предупреждение.
/// </remarks>
internal sealed class MenuCommandRouter : IDisposable
{
    private const string LogPrefix = "[Menu]";
    private readonly ISwiftlyCore _core;
    private readonly MenuSnapshotStore _snapshotStore;
    private readonly IMenuCommandTarget _target;
    private readonly object _lifecycleGate = new();
    private readonly Dictionary<string, Guid> _consoleHooks = new(StringComparer.Ordinal);
    private Guid _chatHook;
    private bool _started;
    private MenuRuntimeSnapshot? _lastSlashWarningSnapshot;

    /// <summary>
    /// Создаёт runtime router без регистрации hooks до вызова <see cref="Start"/>.
    /// </summary>
    /// <param name="core">Swiftly core текущего plugin.</param>
    /// <param name="snapshotStore">Источник атомарного runtime snapshot.</param>
    /// <param name="target">Синхронная runtime-цель открытия меню.</param>
    public MenuCommandRouter(
        ISwiftlyCore core,
        MenuSnapshotStore snapshotStore,
        IMenuCommandTarget target)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// Идемпотентно регистрирует единый chat hook и console aliases текущего snapshot.
    /// </summary>
    public void Start()
    {
        lock (_lifecycleGate)
        {
            if (_started)
            {
                return;
            }

            _chatHook = _core.Command.HookClientChat(OnClientChat);
            _started = true;
            ReconcileConsoleCommandsCore(_snapshotStore.Current);
        }
    }

    /// <summary>
    /// Сверяет нативные console registrations с одним чтением текущего snapshot.
    /// </summary>
    /// <remarks>
    /// Метод следует вызвать после успешной смены snapshot. Уже существующие
    /// callbacks сохраняются: при выполнении каждый из них всё равно делает lookup
    /// в актуальном snapshot, поэтому смена целевого меню не требует перерегистрации.
    /// Новые aliases добавляются до удаления устаревших, что уменьшает окно
    /// недоступности команд при reload.
    /// </remarks>
    public void ReconcileConsoleCommands()
    {
        lock (_lifecycleGate)
        {
            if (!_started)
            {
                return;
            }

            ReconcileConsoleCommandsCore(_snapshotStore.Current);
        }
    }

    /// <summary>
    /// Идемпотентно удаляет все зарегистрированные hooks и commands.
    /// </summary>
    public void Stop()
    {
        lock (_lifecycleGate)
        {
            if (!_started && _chatHook == Guid.Empty && _consoleHooks.Count == 0)
            {
                return;
            }

            _started = false;

            if (_chatHook != Guid.Empty)
            {
                try
                {
                    _core.Command.UnhookClientChat(_chatHook);
                    _chatHook = Guid.Empty;
                }
                catch (Exception exception)
                {
                    // Handle сохраняется, чтобы Dispose мог повторить cleanup.
                    _core.Logger.LogError(
                        exception,
                        "{Prefix} Не удалось удалить chat hook при остановке.",
                        LogPrefix);
                }
            }

            foreach (var (alias, hook) in _consoleHooks.ToArray())
            {
                try
                {
                    _core.Command.UnregisterCommand(hook);
                    _consoleHooks.Remove(alias);
                }
                catch (Exception exception)
                {
                    // Один сломанный native handle не должен помешать очистке
                    // остальных. Неудалённый handle повторно обработает Dispose.
                    _core.Logger.LogError(
                        exception,
                        "{Prefix} Не удалось удалить console alias {Alias} при остановке.",
                        LogPrefix,
                        alias);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    private HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        if (!TryReadChatAlias(text, out var canonicalAlias))
        {
            return HookResult.Continue;
        }

        // Snapshot читается ровно один раз: command и menu обязаны принадлежать
        // одному Release даже при одновременной активации нового snapshot.
        var snapshot = _snapshotStore.Current;
        if (!snapshot.TryGetCommand(MenuCommandKind.Chat, canonicalAlias, out var command))
        {
            return HookResult.Continue;
        }

        MenuOperationResult? result = null;
        var player = _core.PlayerManager.GetPlayer(playerId);
        if (player is { IsValid: true })
        {
            result = TryOpen(player, snapshot, command.MenuKey, canonicalAlias);
        }

        return command.Definition.ChatSuppression switch
        {
            ChatSuppressionMode.OnMatch => HookResult.CancelOriginal,
            ChatSuppressionMode.OnSuccess when result?.IsSuccess == true => HookResult.CancelOriginal,
            _ => HookResult.Continue
        };
    }

    private void OnConsoleCommand(string canonicalAlias, ICommandContext context)
    {
        // Старый native callback может успеть начаться между snapshot swap и
        // reconcile. Lookup в зафиксированном snapshot безопасно отклоняет его.
        var snapshot = _snapshotStore.Current;
        if (!snapshot.TryGetCommand(MenuCommandKind.Console, canonicalAlias, out var command))
        {
            context.Reply($"{LogPrefix} Команда недоступна в активной конфигурации.");
            return;
        }

        if (context.Sender is not { IsValid: true } player)
        {
            context.Reply($"{LogPrefix} Команда доступна только игроку.");
            return;
        }

        var result = TryOpen(player, snapshot, command.MenuKey, canonicalAlias);
        if (!result.IsSuccess)
        {
            context.Reply(ConsoleFailureMessage(result));
        }
    }

    private MenuOperationResult TryOpen(
        IPlayer player,
        MenuRuntimeSnapshot snapshot,
        string menuKey,
        string alias)
    {
        try
        {
            return _target.OpenMenu(player, snapshot, menuKey)
                   ?? MenuOperationResult.Failure(
                       MenuOperationStatus.HandlerFailed,
                       "menu_command_null_result");
        }
        catch (Exception exception)
        {
            _core.Logger.LogError(
                exception,
                "{Prefix} Ошибка выполнения menu alias {Alias} из Release {ReleaseId}.",
                LogPrefix,
                alias,
                snapshot.ReleaseId);
            return MenuOperationResult.Failure(
                MenuOperationStatus.HandlerFailed,
                "menu_command_failed");
        }
    }

    private void ReconcileConsoleCommandsCore(MenuRuntimeSnapshot snapshot)
    {
        LogSlashAliasLimitation(snapshot);

        var desired = snapshot.Commands.Values
            .Where(static command => command.Definition.Kind == MenuCommandKind.Console)
            .Select(static command => command.CanonicalAlias)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var alias in desired)
        {
            if (_consoleHooks.ContainsKey(alias))
            {
                continue;
            }

            try
            {
                var registration = ConsoleRegistration.FromCanonicalAlias(alias);
                var capturedAlias = alias;
                var hook = _core.Command.RegisterCommand(
                    registration.CommandName,
                    context => OnConsoleCommand(capturedAlias, context),
                    registration.RegisterRaw,
                    permission: string.Empty,
                    helpText: "Открыть опубликованное меню Menu.Core");
                _consoleHooks.Add(alias, hook);
            }
            catch (Exception exception)
            {
                _core.Logger.LogError(
                    exception,
                    "{Prefix} Не удалось зарегистрировать console alias {Alias} из Release {ReleaseId}.",
                    LogPrefix,
                    alias,
                    snapshot.ReleaseId);
            }
        }

        foreach (var obsoleteAlias in _consoleHooks.Keys.Where(alias => !desired.Contains(alias)).ToArray())
        {
            var hook = _consoleHooks[obsoleteAlias];
            try
            {
                _core.Command.UnregisterCommand(hook);
                _consoleHooks.Remove(obsoleteAlias);
            }
            catch (Exception exception)
            {
                // Handle остаётся в коллекции: следующий reconcile повторит
                // удаление, а callback уже безопасно проверяет current snapshot.
                _core.Logger.LogError(
                    exception,
                    "{Prefix} Не удалось удалить устаревший console alias {Alias}.",
                    LogPrefix,
                    obsoleteAlias);
            }
        }
    }

    private void LogSlashAliasLimitation(MenuRuntimeSnapshot snapshot)
    {
        if (ReferenceEquals(_lastSlashWarningSnapshot, snapshot))
        {
            return;
        }

        _lastSlashWarningSnapshot = snapshot;
        var slashAliasCount = snapshot.Commands.Values.Count(static command =>
            command.Definition.Kind == MenuCommandKind.Chat &&
            command.CanonicalAlias.StartsWith("/", StringComparison.Ordinal));
        if (slashAliasCount == 0)
        {
            return;
        }

        _core.Logger.LogWarning(
            "{Prefix} Release {ReleaseId} содержит {AliasCount} chat alias с '/'. " +
            "CS2 или другой plugin может обработать slash-команду до Swiftly HookClientChat; " +
            "открытие и suppression для таких aliases не гарантируются.",
            LogPrefix,
            snapshot.ReleaseId,
            slashAliasCount);
    }

    private static bool TryReadChatAlias(string? text, out string canonicalAlias)
    {
        canonicalAlias = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var remaining = text.AsSpan().TrimStart();
        if (remaining.IsEmpty || remaining[0] is not ('!' or '/'))
        {
            return false;
        }

        var tokenLength = 0;
        while (tokenLength < remaining.Length && !char.IsWhiteSpace(remaining[tokenLength]))
        {
            tokenLength++;
        }

        try
        {
            // Form C сводит визуально одинаковые кириллические aliases к тому же
            // lookup key, который был подготовлен compiler при активации Release.
            canonicalAlias = MenuIdentifier.CanonicalizeAlias(
                remaining[..tokenLength].ToString());
            return canonicalAlias.Length > 1;
        }
        catch (ArgumentException)
        {
            // Некорректная UTF-16 последовательность не является командой и не
            // должна ломать обработку остальных chat hooks.
            canonicalAlias = string.Empty;
            return false;
        }
    }

    private static string ConsoleFailureMessage(MenuOperationResult result)
    {
        var reason = result.Status switch
        {
            MenuOperationStatus.NotFound => "меню не найдено в активной конфигурации",
            MenuOperationStatus.AccessDenied => "нет доступа к меню",
            MenuOperationStatus.ProviderOffline => "необходимый plugin сейчас недоступен",
            MenuOperationStatus.Unsupported => "меню использует неподдерживаемую возможность",
            MenuOperationStatus.ValidationFailed => "конфигурация меню отклонена",
            _ => "внутренняя ошибка выполнения"
        };
        return $"{LogPrefix} Не удалось открыть меню: {reason}.";
    }

    private readonly record struct ConsoleRegistration(string CommandName, bool RegisterRaw)
    {
        public static ConsoleRegistration FromCanonicalAlias(string alias)
        {
            return alias.StartsWith("sw_", StringComparison.Ordinal)
                ? new ConsoleRegistration(alias[3..], RegisterRaw: false)
                : new ConsoleRegistration(alias, RegisterRaw: true);
        }
    }
}
