using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;

namespace ZombiePlague.Core.Catalog;

internal sealed class ZombieCatalogLifecycle(ISwiftlyCore core, ZombieCatalogService catalog) : IDisposable
{
    private readonly List<Guid> _commands = [];
    private bool _started;
    private bool _precacheRefreshed;

    public void Start()
    {
        if (_started) return;
        catalog.Initialize();
        _started = true;
        core.Event.OnPrecacheResource += OnPrecache;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        foreach (var name in new[] { "zp_classes_reload", "zp_abilities_reload" })
            _commands.Add(core.Command.RegisterCommand(name, context =>
            {
                _ = catalog.RefreshAsync();
                context.Reply("Каталог классов и способностей обновляется — zp_classes_status покажет результат");
            }, registerRaw: true, permission: "zombie_plague.admin.classes"));
        _commands.Add(core.Command.RegisterCommand("zp_classes_status", context =>
        {
            var state = catalog.Current;
            context.Reply($"ZombieCatalog: source={state.Source}, version={state.Version}, classes={state.Document.Classes.Count}, abilities={state.Document.Abilities.Count}, pending_resources={catalog.PendingResourceVersion?.ToString() ?? "none"}");
        }, registerRaw: true, permission: "zombie_plague.admin.classes"));
    }

    private void OnPrecache(IOnPrecacheResourceEvent args)
    {
        // На этапе загрузки карты ждём ограниченный тайм-аутом запрос, чтобы новые модели попали в её manifest
        catalog.RefreshAsync().GetAwaiter().GetResult();
        _precacheRefreshed = true;
        foreach (var resource in catalog.PrepareResourcesForMap()) args.AddItem(resource);
    }

    private void OnMapLoad(IOnMapLoadEvent args)
    {
        if (!_precacheRefreshed) _ = catalog.RefreshAsync();
        _precacheRefreshed = false;
    }

    private void OnMapUnload(IOnMapUnloadEvent args) => _precacheRefreshed = false;

    public void Dispose()
    {
        if (_started)
        {
            _started = false;
            core.Event.OnPrecacheResource -= OnPrecache;
            core.Event.OnMapLoad -= OnMapLoad;
            core.Event.OnMapUnload -= OnMapUnload;
            foreach (var command in _commands) core.Command.UnregisterCommand(command);
            _commands.Clear();
        }
        catalog.Dispose();
    }
}
