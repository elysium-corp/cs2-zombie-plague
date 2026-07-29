using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Menus.Contracts;
using ZombiePlague.Core.Store.Contracts;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Data.Menus;

internal sealed class ZClassMenu(
    ISwiftlyCore core,
    IOptions<ZClassConfig> config,
    IPlayerRepository playerRepository
) : IZClassMenu
{
    private Guid _commandGuid = Guid.Empty;

    public void RegisterMenu()
    {
        if (_commandGuid != Guid.Empty)
        {
            return;
        }

        _commandGuid = core.Command.RegisterCommand(
            commandName: "class",
            handler: context =>
            {
                if (context.Sender is { IsValid: true } player)
                {
                    Open(player);
                }
            },
            registerRaw: true
        );
    }

    public void UnregisterMenu()
    {
        if (_commandGuid == Guid.Empty)
        {
            return;
        }

        core.Command.UnregisterCommand(_commandGuid);
        _commandGuid = Guid.Empty;
    }

    public void Open(IPlayer player)
    {
        if (!player.IsValid)
        {
            return;
        }

        core.MenusAPI.OpenMenuForPlayer(player, CreateMenu());
    }

    private IMenuAPI CreateMenu()
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Зомби-классы")
            .EnableSound();

        foreach (var zClass in GetAvailableClasses())
        {
            AddZClassOption(builder, zClass);
        }

        return builder.Build();
    }

    private IEnumerable<IZClassConfig> GetAvailableClasses()
    {
        var classes = config.Value;

        return new IZClassConfig[]
        {
            classes.Cleric,
            classes.Assassin,
            classes.Heavy,
            classes.Hunter,
            classes.Smoker
        }.Where(zClass => zClass.Enabled);
    }

    private void AddZClassOption(
        IMenuBuilderAPI builder,
        IZClassConfig zClass
    )
    {
        var description = HtmlHelper.TextWithColor(
            zClass.Description,
            "#FFFF00"
        );
        var button = new ButtonMenuOption(
            $"{zClass.DisplayName} {description}"
        );

        button.Click += (_, args) =>
        {
            var player = args.Player;

            if (player.IsValid)
            {
                playerRepository.SetZClassId(player, zClass.InternalName);
                core.MenusAPI.CloseActiveMenu(player);
            }

            return ValueTask.CompletedTask;
        };

        builder.AddOption(button);
    }
}