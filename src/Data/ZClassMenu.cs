using CS2ZombiePlague.Data.ZClasses;
using CS2ZombiePlague.Di;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data;

public class ZClassMenu(ISwiftlyCore core)
{
    private readonly Dictionary<int, IZClass> _playersZClass = new();
    private IMenuAPI _menu;

    public void RegisterHooks()
    {
        _menu = CreateMenu();
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
    }

    public IZClass GetPlayerZClass(int playerId)
    {
        if (_playersZClass.TryGetValue(playerId, out var zClass))
        {
            return zClass;
        }

        return _playersZClass[playerId] = DependencyManager.GetService<ZCleric>();
    }

    private IMenuAPI CreateMenu()
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Зомби-классы")
            .EnableSound();

        AddZClassOption<ZCleric>(builder);
        AddZClassOption<ZAssassin>(builder);
        AddZClassOption<ZHeavy>(builder);
        AddZClassOption<ZHunter>(builder);
        AddZClassOption<ZSmoker>(builder);

        return builder.Build();
    }
    
    private void AddZClassOption<T>(IMenuBuilderAPI builder) where T : IZClass
    {
        var zClass = DependencyManager.GetService<T>();
        var button = new ButtonMenuOption($"{zClass.DisplayName} {zClass.Description}");

        button.Click += (_, args) =>
        {
            var playerId = @args.Player.PlayerID;
            _playersZClass[playerId] = zClass;
            
            core.MenusAPI.CloseMenuForPlayer(@args.Player, _menu);
            
            core.PlayerManager.SendCenterAsync($"{zClass.DisplayName} успешно выбран!");
            
            return ValueTask.CompletedTask;
        };
        builder.AddOption(button);
    }

    private HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        if (@event.Text == "!class")
        {
            core.MenusAPI.OpenMenuForPlayer(@event.UserIdPlayer, _menu);
        }

        return HookResult.Continue;
    }
}