using Menu.Api.Extensions;

namespace Menu.Core.Extensions;

internal sealed class MenuExtensionRegistry : IMenuExtensionRegistry, IMenuExtensionDispatcher
{
    private readonly Dictionary<string, List<MenuExtensionHandler>> _handlers = new(StringComparer.Ordinal);

    private readonly Lock _sync = new();

    public IDisposable Subscribe(string menuId, MenuExtensionHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuId);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_sync)
        {
            if (!_handlers.TryGetValue(menuId, out var handlers))
            {
                handlers = [];
                _handlers.Add(menuId, handlers);
            }

            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(menuId, handler));
    }

    public void Dispatch(string menuId, MenuExtensionContext context)
    {
        MenuExtensionHandler[] handlers;

        lock (_sync)
        {
            if (!_handlers.TryGetValue(menuId, out var registeredHandlers))
            {
                return;
            }

            handlers = registeredHandlers.ToArray();
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler(context);
            }
            catch (Exception exception)
            {
                // вывод в логи ошибки, почему не удалось отобразить пункт меню
            }
        }
    }

    private void Unsubscribe(string menuId, MenuExtensionHandler handler)
    {
        lock (_sync)
        {
            if (!_handlers.TryGetValue(menuId, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _handlers.Remove(menuId);
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            Interlocked
                .Exchange(ref _unsubscribe, null)
                ?.Invoke();
        }
    }
}