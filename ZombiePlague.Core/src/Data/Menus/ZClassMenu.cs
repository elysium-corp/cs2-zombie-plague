using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Menus.Contracts;
using ZombiePlague.Core.Data.Zombies;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Data.Menus;

internal class ZClassMenu(ISwiftlyCore core, IZClassFactory zClassFactory) : IZClassMenu
{
    private readonly Dictionary<IPlayer, IZClass> _playersZClass = new();

    public void RegisterMenu()
    {
        core.Command.RegisterCommand(
            commandName: "class",
            handler: (args)=>
            {
                if (args.Sender != null) Open(args.Sender);
            },
            registerRaw: true
        );
    }

    public void Open(IPlayer player)
    {
        core.MenusAPI.OpenMenuForPlayer(player, CreateMenu());
    }

    public IZClass GetPlayerZClass(IPlayer player)
    {
        if (_playersZClass.TryGetValue(player, out var zClass))
        {
            return zClass;
        }

        return _playersZClass[player] = zClassFactory.Create<ZCleric>();
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
    
    private void AddZClassOption<TClass>(IMenuBuilderAPI builder) where TClass : IZClass
    {
        var zClass = zClassFactory.Create<TClass>();
        var button = new ButtonMenuOption($"{zClass.DisplayName} {HtmlHelper.TextWithColor(zClass.Description, "#FFFF00")}");

        button.Click += (_, args) =>
        {
            var player = @args.Player;
            
            _playersZClass[player] = zClass;
            
            core.MenusAPI.CloseActiveMenu(args.Player);
            
            core.PlayerManager.SendCenterAsync($"{zClass.DisplayName} успешно выбран!");
            
            return ValueTask.CompletedTask;
        };
        
        builder.AddOption(button);
    }
}