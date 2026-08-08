using Menu.Api.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

public abstract class DynamicOptionsMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher
) : MenuBase(core)
{
    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);
        var options = new MenuOptionsCollection();

        BuildOptions(player, options);

        extensionDispatcher.Dispatch(
            Id,
            new MenuExtensionContext(
                player,
                options
            )
        );

        foreach (var option in options.Build())
        {
            builder.AddOption(option);
        }

        return builder.Build();
    }

    protected virtual void BuildOptions(IPlayer player, MenuOptionsCollection options) { }
}