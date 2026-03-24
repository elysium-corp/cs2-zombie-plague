using ZPCore.Data.Extensions;
using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace ZPCore.Data.Lifecycle;

internal sealed class LifecycleManager(ServiceLifecycleManager serviceLifecycleManager, PlayerLifecycleManager playerLifecycleManager)
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();

    private Guid _guidOnPlayerDeathPost;
    private Guid _guidOnEventPlayerSpawnPost;
    private Guid _guidOnEventPlayerDisconnectPost;

    public void Initialize()
    {
        _guidOnPlayerDeathPost = _core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeathPost);
        _guidOnEventPlayerSpawnPost = _core.GameEvent.HookPost<EventPlayerSpawn>(OnEventPlayerSpawnPost);
        _guidOnEventPlayerDisconnectPost = _core.GameEvent.HookPost<EventPlayerDisconnect>(OnEventPlayerDisconnectPost);

        _core.Event.OnMapLoad += OnMapLoad;
        _core.Event.OnMapUnload += OnMapUnload;
        _core.Event.OnStartupServer += OnStartupServer;
    }
    
    public void Dispose()
    {
        _core.GameEvent.Unhook(_guidOnPlayerDeathPost);
        _core.GameEvent.Unhook(_guidOnEventPlayerSpawnPost);
        _core.GameEvent.Unhook(_guidOnEventPlayerDisconnectPost);
        
        _core.Event.OnMapLoad -= OnMapLoad;
        _core.Event.OnMapUnload -= OnMapUnload;
        _core.Event.OnStartupServer -= OnStartupServer;
        
        serviceLifecycleManager.Dispose();
        playerLifecycleManager.Dispose();
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        playerLifecycleManager.RemoveAll();
    }

    private void OnMapUnload(IOnMapUnloadEvent @event)
    {
        playerLifecycleManager.RemoveAll();
    }

    private void OnStartupServer()
    {
        playerLifecycleManager.RemoveAll();
    }

    private HookResult OnPlayerDeathPost(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;

        if (player == null)
        {
            return HookResult.Continue;
        }

        var playerWithLifecycle = player.GetLifecycle();
        playerLifecycleManager.Remove(playerWithLifecycle);
        
        return HookResult.Continue;
    }

    private HookResult OnEventPlayerSpawnPost(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player is not { IsValid: true } || player.IsFakeClient)
        {
            return HookResult.Continue;
        }
        
        var playerWithLifecycle = player.GetLifecycle();
        playerLifecycleManager.Add(playerWithLifecycle);
        
        return HookResult.Continue;
    }

    private HookResult OnEventPlayerDisconnectPost(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player == null)
        {
            return HookResult.Continue;
        }
        
        var playerWithLifecycle = player.GetLifecycle();
        playerLifecycleManager.Remove(playerWithLifecycle);
        
        return HookResult.Continue;
    }
}